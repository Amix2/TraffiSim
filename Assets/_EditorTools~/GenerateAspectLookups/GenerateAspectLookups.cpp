// GenerateAspectLookups.cpp
// Usage: GenerateAspectLookups.exe <item_path> <project_root>
//
// For every *.cs file targeted:
//   - finds each `struct X : IAspect`
//   - collects fields of type RefRW<T> / RefRO<T> (with optional [Optional] attribute)
//   - deletes any existing `public struct Lookup { ... }` inside the aspect
//   - regenerates the Lookup struct at the top of the aspect body
//
// Requires /std:c++17 or later.

#include <cstdio>
#include <filesystem>
#include <fstream>
#include <regex>
#include <sstream>
#include <string>
#include <vector>

namespace fs = std::filesystem;

struct AspectField
{
    std::string componentType;  // e.g. "LocalTransform" (namespace stripped)
    std::string fieldName;      // e.g. "LocalTransformRW"
    bool        rw = false;     // RefRW vs RefRO
    bool        optional = false;
};

static std::string ReadFile(const fs::path& p)
{
    std::ifstream in(p, std::ios::binary);
    std::stringstream ss;
    ss << in.rdbuf();
    return ss.str();
}

static void WriteFile(const fs::path& p, const std::string& text)
{
    std::ofstream out(p, std::ios::binary);
    out << text;
}

// Given index of '{' in s, return index of its matching '}' (or npos).
// Skips braces inside strings, chars, and // or /* */ comments.
static size_t FindMatchingBrace(const std::string& s, size_t open)
{
    int depth = 0;
    for (size_t i = open; i < s.size(); ++i)
    {
        char c = s[i];
        if (c == '/' && i + 1 < s.size() && s[i + 1] == '/')
        {
            i = s.find('\n', i);
            if (i == std::string::npos) return std::string::npos;
        }
        else if (c == '/' && i + 1 < s.size() && s[i + 1] == '*')
        {
            i = s.find("*/", i + 2);
            if (i == std::string::npos) return std::string::npos;
            ++i; // lands on '/'
        }
        else if (c == '"' || c == '\'')
        {
            char quote = c;
            for (++i; i < s.size(); ++i)
            {
                if (s[i] == '\\') { ++i; continue; }
                if (s[i] == quote) break;
            }
        }
        else if (c == '{') ++depth;
        else if (c == '}')
        {
            if (--depth == 0) return i;
        }
    }
    return std::string::npos;
}

// Convert bare \n in generated text to \r\n when the source file uses CRLF.
static std::string MatchNewlines(const std::string& generated, bool crlf)
{
    if (!crlf) return generated;
    std::string out;
    out.reserve(generated.size() + generated.size() / 16);
    for (char c : generated)
    {
        if (c == '\n') out += "\r\n";
        else out += c;
    }
    return out;
}

static std::string StripNamespace(const std::string& type)
{
    size_t dot = type.rfind('.');
    return dot == std::string::npos ? type : type.substr(dot + 1);
}

// Indentation of the line containing position pos.
static std::string LineIndent(const std::string& s, size_t pos)
{
    size_t lineStart = s.rfind('\n', pos);
    lineStart = (lineStart == std::string::npos) ? 0 : lineStart + 1;
    std::string indent;
    for (size_t i = lineStart; i < s.size() && (s[i] == ' ' || s[i] == '\t'); ++i)
        indent += s[i];
    return indent;
}

// Remove an existing `struct Lookup { ... }` from body. Returns true if removed.
static bool RemoveExistingLookup(std::string& body)
{
    static const std::regex lookupDecl(
        R"((?:public\s+|internal\s+|readonly\s+|partial\s+)*struct\s+Lookup\b)");

    std::smatch m;
    if (!std::regex_search(body, m, lookupDecl))
        return false;

    size_t declStart = (size_t)m.position(0);
    size_t open = body.find('{', declStart + m.length(0));
    if (open == std::string::npos) return false;
    size_t close = FindMatchingBrace(body, open);
    if (close == std::string::npos) return false;

    // Extend start back to beginning of line, end past trailing newline.
    size_t lineStart = body.rfind('\n', declStart);
    lineStart = (lineStart == std::string::npos) ? 0 : lineStart + 1;
    size_t end = close + 1;
    while (end < body.size() && (body[end] == '\r' || body[end] == '\n'))
        ++end;

    body.erase(lineStart, end - lineStart);
    return true;
}

static std::vector<AspectField> CollectFields(const std::string& body)
{
    static const std::regex fieldRe(
        R"((\[\s*OptionalLookup\s*\]\s*)?(?:public\s+|internal\s+|private\s+|readonly\s+)*Ref(RW|RO)\s*<\s*([\w\.]+)\s*>\s+(\w+)\s*;)");

    std::vector<AspectField> fields;
    for (auto it = std::sregex_iterator(body.begin(), body.end(), fieldRe);
        it != std::sregex_iterator(); ++it)
    {
        AspectField f;
        f.optional = (*it)[1].matched;
        f.rw = (*it)[2].str() == "RW";
        f.componentType = StripNamespace((*it)[3].str());
        f.fieldName = (*it)[4].str();
        fields.push_back(f);
    }
    return fields;
}

static std::string DetectEntityFieldName(const std::string& body)
{
    static const std::regex entityRe(R"(public\s+Entity\s+(\w+)\s*;)");
    std::smatch m;
    if (std::regex_search(body, m, entityRe))
        return m[1].str();
    return "Entity";
}

static std::string GenerateLookup(const std::string& aspectName,
    const std::string& entityField,
    const std::vector<AspectField>& fields,
    const std::string& ind) // indent of aspect members
{
    const std::string i1 = ind;         // struct Lookup
    const std::string i2 = ind + "    ";
    const std::string i3 = ind + "        ";

    // One slot per component type (two fields of the same T share a slot).
    std::vector<std::string> slotTypes;
    for (const auto& f : fields)
    {
        bool seen = false;
        for (const auto& t : slotTypes) if (t == f.componentType) { seen = true; break; }
        if (!seen) slotTypes.push_back(f.componentType);
    }

    std::ostringstream o;
    o << i1 << "public struct Lookup\n" << i1 << "{\n";

    for (const auto& t : slotTypes)
        o << i2 << "public LookupSlot<" << t << "> " << t << "Lookup;\n";

    auto forwarder = [&](const std::string& signature, const std::string& call)
        {
            o << i2 << signature << "\n" << i2 << "{\n";
            for (const auto& t : slotTypes)
                o << i3 << t << "Lookup." << call << ";\n";
            o << i2 << "}\n";
        };

    forwarder("public void Initialize(ref SystemState state)", "Initialize(ref state)");
    forwarder("public void Initialize(ComponentSystemBase system)", "Initialize(system)");
    forwarder("public void Update(ref SystemState state)", "Update(ref state)");
    forwarder("public void Update(SystemBase system)", "Update(system)");

    o << i2 << "public " << aspectName << " this[Entity e] => new " << aspectName << "\n"
        << i2 << "{\n"
        << i3 << entityField << " = e";
    for (const auto& f : fields)
    {
        o << ",\n" << i3 << f.fieldName << " = " << f.componentType << "Lookup.Bind"
            << (f.rw ? "RW" : "RO") << (f.optional ? "Optional" : "") << "(e)";
    }
    o << "\n" << i2 << "};\n";

    o << i1 << "}\n";
    return o.str();
}

// Process one file. Returns true if the file was modified.
static bool ProcessFile(const fs::path& path)
{
    std::string text = ReadFile(path);

    // Find every `struct X : <bases containing IAspect>`.
    static const std::regex aspectRe(R"(struct\s+(\w+)\s*:\s*([^\{]*))");

    struct AspectSpan { std::string name; size_t open, close; };
    std::vector<AspectSpan> aspects;

    for (auto it = std::sregex_iterator(text.begin(), text.end(), aspectRe);
        it != std::sregex_iterator(); ++it)
    {
        if (!std::regex_search((*it)[2].str(), std::regex(R"(\bIAspect\b)")))
            continue;
        size_t open = text.find('{', (size_t)it->position(0) + it->length(0) - 1);
        if (open == std::string::npos) continue;
        size_t close = FindMatchingBrace(text, open);
        if (close == std::string::npos) continue;
        aspects.push_back({ (*it)[1].str(), open, close });
    }

    if (aspects.empty())
        return false;

    bool modified = false;

    // Process back-to-front so earlier spans stay valid.
    for (auto a = aspects.rbegin(); a != aspects.rend(); ++a)
    {
        std::string body = text.substr(a->open + 1, a->close - a->open - 1);

        RemoveExistingLookup(body);
        std::vector<AspectField> fields = CollectFields(body);
        if (fields.empty())
        {
            std::printf("  %s: no RefRW/RefRO fields, skipped\n", a->name.c_str());
            continue;
        }

        std::string entityField = DetectEntityFieldName(body);
        std::string memberIndent = LineIndent(text, a->open) + "    ";
        bool crlf = text.find("\r\n") != std::string::npos;
        std::string lookup = MatchNewlines(
            GenerateLookup(a->name, entityField, fields, memberIndent), crlf);

        // Insert generated Lookup at the top of the (cleaned) body.
        size_t insertAt = 0;
        while (insertAt < body.size() && (body[insertAt] == '\r' || body[insertAt] == '\n'))
            ++insertAt;
        body = body.substr(0, insertAt) + lookup + body.substr(insertAt);
        if (insertAt == 0) body = (crlf ? "\r\n" : "\n") + body;

        text = text.substr(0, a->open + 1) + body + text.substr(a->close);
        modified = true;
        std::printf("  %s: generated Lookup with %zu field(s)\n", a->name.c_str(), fields.size());
    }

    if (modified)
        WriteFile(path, text);
    return modified;
}

int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        std::printf("usage: GenerateAspectLookups.exe <item_path> [project_root]\n");
        return 1;
    }

    fs::path item(argv[1]);
    int changed = 0;

    auto handle = [&](const fs::path& p)
        {
            std::printf("%s\n", p.string().c_str());
            if (ProcessFile(p)) ++changed;
        };

    if (fs::is_directory(item))
    {
        for (auto& e : fs::recursive_directory_iterator(item))
            if (e.is_regular_file() && e.path().extension() == ".cs")
                handle(e.path());
    }
    else if (fs::exists(item))
    {
        handle(item);
    }
    else
    {
        std::printf("path not found: %s\n", item.string().c_str());
        return 1;
    }

    std::printf("Done. %d file(s) modified.\n", changed);
    return 0;
}