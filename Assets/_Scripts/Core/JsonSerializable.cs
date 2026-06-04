// JsonSerializable.cs
// Requires: Newtonsoft.Json
// Targets: .NET 6+ / Unity 2021.2+
#nullable enable
//
// ---- Quick usage ----
//
//   class Enemy : JsonSerializable
//   {
//       public string  Name   = "";       // key = "Name"  (required)
//       public int     Health = 0;        // key = "Health" (required)
//
//       [JsonKey("pos")]
//       public Vector3 Position;          // key = "pos"   (required)
//
//       [JsonOptional]
//       public string  Tag = "";          // key = "Tag"   (optional)
//
//       [JsonIgnore]
//       public int     _cache;            // skipped entirely
//
//       [JsonSelf]
//       public JObject Raw = new();       // receives the whole source JObject
//   }
//
//   var e = JsonSerializable.FromJson<Enemy>(jobject);
//   var e = JsonSerializable.FromJsonString<Enemy>(text);
//   JObject j = e.ToJson();
//
// ---- Inheritance ----
//   Just inherit normally. Reflection walks the full hierarchy (base fields first).
//
//       class Soldier : Enemy { public string Rank = ""; }
//       var s = JsonSerializable.FromJson<Soldier>(jobject);   // reads Enemy + Soldier fields
//
// ---- Custom converters ----
//   JsonConverters.Register<Vec3>(
//       read:  j => new Vec3 { x = j[0].Value<float>(), y = j[1].Value<float>(), z = j[2].Value<float>() },
//       write: v => new JArray(v.x, v.y, v.z)
//   );

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

// ============================================================
//  Attributes
// ============================================================

/// Override the JSON key for a field or property.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class JsonKeyAttribute : Attribute
{
    public string Key { get; }

    public JsonKeyAttribute(string key) => Key = key;
}

/// Optional member: if the key is absent the member keeps its default value.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class JsonOptionalAttribute : Attribute
{ }

/// Skip this member entirely.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class JsonIgnoreAttribute : Attribute
{ }

/// Store the entire source JObject into this JObject member.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class JsonSelfAttribute : Attribute
{ }

// ============================================================
//  JsonConverters — registry for custom type read/write hooks
// ============================================================

public static class JsonConverters
{
    private static readonly Dictionary<Type, Func<JToken, object>> _readers = new();
    private static readonly Dictionary<Type, Func<object, JToken>> _writers = new();

    public static Guid GuidFromInt(int value)
    {
        return new Guid(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        //             ^int   ^short ^short ^8 bytes...
    }

    public static Guid GuidFromString(string input)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash); // Guid takes a 16-byte array
    }

    static JsonConverters()
    {
        Register<UnityEngine.Vector2>(
            read: j => new UnityEngine.Vector2(j[0]?.Value<float>() ?? 0f, j[1]?.Value<float>() ?? 0f),
            write: v => new JArray(v.x, v.y)
        );
        Register<UnityEngine.Vector3>(
            read: j => new UnityEngine.Vector3(j[0]?.Value<float>() ?? 0f, j[1]?.Value<float>() ?? 0f, j[2]?.Value<float>() ?? 0f),
            write: v => new JArray(v.x, v.y, v.z)
        );
        Register<UnityEngine.Vector4>(
            read: j => new UnityEngine.Vector4(j[0]?.Value<float>() ?? 0f, j[1]?.Value<float>() ?? 0f, j[2]?.Value<float>() ?? 0f, j[3]?.Value<float>() ?? 0f),
            write: v => new JArray(v.x, v.y, v.z, v.w)
        );
        Register<UnityEngine.Color>(
            read: j => new UnityEngine.Color(j[0]?.Value<float>() ?? 0f, j[1]?.Value<float>() ?? 0f, j[2]?.Value<float>() ?? 0f, j.Count() > 3 ? j[3]?.Value<float>() ?? 1f : 1f),
            write: c => new JArray(c.r, c.g, c.b, c.a)
        );
        Register<UnityEngine.Quaternion>(
            read: j => new UnityEngine.Quaternion(j[0]?.Value<float>() ?? 0f, j[1]?.Value<float>() ?? 0f, j[2]?.Value<float>() ?? 0f, j[3]?.Value<float>() ?? 1f),
            write: q => new JArray(q.x, q.y, q.z, q.w)
        );
        Register<Guid>(
           read: j =>
           {
               if (j.Type == JTokenType.String)
               {
                   string? value = j.Value<string>();
                   if (value == null)
                       return Guid.Empty;
                   if (Guid.TryParse(value, out Guid result))
                       return result;
                   return GuidFromString(value);
               }
               if (j.Type == JTokenType.Integer)
               {
                   return GuidFromInt(j.Value<int>());
               }
               return Guid.Empty;
           }
           ,
           write: g => new JValue(g.ToString())
       );
    }

    public static void Register<T>(Func<JToken, T> read, Func<T, JToken> write)
    {
        _readers[typeof(T)] = tok => read(tok)!;
        _writers[typeof(T)] = obj => write((T)obj);
    }

    public static bool TryRead(Type t, JToken token, out object? value)
    {
        if (_readers.TryGetValue(t, out var fn)) { value = fn(token); return true; }
        value = null;
        return false;
    }

    public static bool TryWrite(Type t, object obj, out JToken? token)
    {
        if (_writers.TryGetValue(t, out var fn)) { token = fn(obj); return true; }
        token = null;
        return false;
    }
}

// ============================================================
//  MemberAccessor — wraps a FieldInfo or PropertyInfo uniformly
// ============================================================

internal sealed class MemberAccessor
{
    private readonly FieldInfo? _field;
    private readonly PropertyInfo? _prop;

    public Type MemberType { get; }
    public bool IsSelf { get; }
    public bool IsOptional { get; }
    public bool IsRequired => !IsOptional && !IsSelf;
    public string JsonKey { get; }

    public MemberAccessor(FieldInfo f)
    {
        _field = f;
        MemberType = f.FieldType;
        IsSelf = f.IsDefined(typeof(JsonSelfAttribute), inherit: true);
        IsOptional = f.IsDefined(typeof(JsonOptionalAttribute), inherit: true);
        JsonKey = f.GetCustomAttribute<JsonKeyAttribute>()?.Key ?? f.Name;
    }

    public MemberAccessor(PropertyInfo p)
    {
        _prop = p;
        MemberType = p.PropertyType;
        IsSelf = p.IsDefined(typeof(JsonSelfAttribute), inherit: true);
        IsOptional = p.IsDefined(typeof(JsonOptionalAttribute), inherit: true);
        JsonKey = p.GetCustomAttribute<JsonKeyAttribute>()?.Key ?? p.Name;
    }

    public object? GetValue(object obj)
        => _field != null ? _field.GetValue(obj) : _prop!.GetValue(obj);

    public void SetValue(object obj, object? value)
    {
        if (_field != null) _field.SetValue(obj, value);
        else _prop!.SetValue(obj, value);
    }
}

// ============================================================
//  ReflectionCache — discovers + caches members per type
// ============================================================

internal static class ReflectionCache
{
    private static readonly Dictionary<Type, List<MemberAccessor>> _cache = new();
    private static readonly object _lock = new();

    public static List<MemberAccessor> GetMembers(Type t)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(t, out var cached)) return cached;

            var members = new List<MemberAccessor>();
            var seen = new HashSet<string>();

            // Walk root -> leaf so base fields are emitted before derived fields.
            var chain = new Stack<Type>();
            for (Type? cur = t; cur != null && cur != typeof(object) && cur != typeof(JsonSerializable); cur = cur.BaseType)
                chain.Push(cur);

            foreach (Type level in chain)
            {
                foreach (var f in level.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (f.IsDefined(typeof(JsonIgnoreAttribute), inherit: false)) continue;
                    if (!seen.Add(f.Name)) continue;   // shadowed by a derived 'new' member
                    members.Add(new MemberAccessor(f));
                }

                foreach (var p in level.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!p.CanRead || !p.CanWrite) continue;
                    if (p.GetIndexParameters().Length > 0) continue;   // skip indexers
                    if (p.IsDefined(typeof(JsonIgnoreAttribute), inherit: false)) continue;
                    if (!seen.Add(p.Name)) continue;
                    members.Add(new MemberAccessor(p));
                }
            }

            _cache[t] = members;
            return members;
        }
    }
}

// ============================================================
//  ValueIO — read/write a single value of any supported type
// ============================================================

internal static class ValueIO
{
    internal static bool IsJsonSerializable(Type t)
        => typeof(JsonSerializable).IsAssignableFrom(t);

    internal static object? Read(Type targetType, JToken tok)
    {
        // Explicit null -> default
        if (tok.Type == JTokenType.Null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        // Custom converter wins
        if (JsonConverters.TryRead(targetType, tok, out object? conv))
            return conv;

        // Nested JsonSerializable
        if (tok is JObject jo && IsJsonSerializable(targetType))
        {
            var nested = (JsonSerializable)Activator.CreateInstance(targetType)!;
            JsonSerializable.Populate(nested, jo);
            return nested;
        }

        // List<T>
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type elem = targetType.GetGenericArguments()[0];
            var list = (System.Collections.IList)Activator.CreateInstance(targetType)!;
            foreach (var item in (JArray)tok)
                list.Add(Read(elem, item));
            return list;
        }

        // Primitive / string / enum / plain JObject member
        return tok.ToObject(targetType);
    }

    internal static JToken Write(object? value)
    {
        if (value is null) return JValue.CreateNull();

        // Nested serializable — runtime type drives field discovery
        if (value is JsonSerializable js) return js.ToJson();

        Type t = value.GetType();

        if (JsonConverters.TryWrite(t, value, out JToken? written))
            return written!;

        if (value is JObject existing) return existing;

        if (value is System.Collections.IEnumerable en && value is not string)
        {
            var arr = new JArray();
            foreach (var item in en)
                arr.Add(Write(item));
            return arr;
        }

        return JToken.FromObject(value);
    }
}

// ============================================================
//  JsonSerializable — non-generic base (inheritance-friendly)
// ============================================================

public abstract class JsonSerializable
{
    // ----------------------------------------------------------
    //  Serialize  (uses the runtime type, so derived fields are
    //  always included even through a base-typed reference)
    // ----------------------------------------------------------

    public JObject ToJson()
    {
        var j = new JObject();
        foreach (var m in ReflectionCache.GetMembers(GetType()))
        {
            if (m.IsSelf) continue;   // [JsonSelf] members aren't written back out
            j[m.JsonKey] = ValueIO.Write(m.GetValue(this));
        }
        return j;
    }

    public string ToJsonString(bool indented = false)
        => ToJson().ToString(indented ? Formatting.Indented : Formatting.None);

    // ----------------------------------------------------------
    //  Deserialize
    // ----------------------------------------------------------

    public static T FromJson<T>(JObject j) where T : JsonSerializable, new()
    {
        var obj = new T();
        Populate(obj, j);
        return obj;
    }

    public static T FromJsonString<T>(string json) where T : JsonSerializable, new()
        => FromJson<T>(JObject.Parse(json));

    public static T FromJsonFile<T>(string path) where T : JsonSerializable, new()
        => FromJson<T>(JObject.Parse(File.ReadAllText(path)));   // ReadAllText auto-detects UTF-8 BOM

    // Shared population routine — also used for nested objects.
    internal static void Populate(JsonSerializable obj, JObject j)
    {
        foreach (var m in ReflectionCache.GetMembers(obj.GetType()))
        {
            if (m.IsSelf)
            {
                if (m.MemberType == typeof(JObject))
                    m.SetValue(obj, j);
                continue;
            }

            if (!j.TryGetValue(m.JsonKey, out JToken? tok))
            {
                if (m.IsRequired)
                    throw new InvalidDataException(
                        $"FromJson ({obj.GetType().Name}): missing required field '{m.JsonKey}'");
                continue;   // optional -> keep default value
            }

            m.SetValue(obj, ValueIO.Read(m.MemberType, tok));
        }
    }

    // ----------------------------------------------------------
    //  CopyFrom — shallow copy of all reflected members from
    //  another instance of the SAME runtime type. Handy for the
    //  "construct typed action from a generic action" pattern.
    // ----------------------------------------------------------

    protected void CopyFrom(JsonSerializable other)
    {
        foreach (var m in ReflectionCache.GetMembers(GetType()))
            m.SetValue(this, m.GetValue(other));
    }
}