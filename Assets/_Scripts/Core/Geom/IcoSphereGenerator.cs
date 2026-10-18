using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates an icosphere Mesh with a diameter of 1 m (radius 0.5),
/// with smooth normals, equirectangular UVs (seam and poles fixed) and tangents.
/// </summary>
public static class IcoSphereGenerator
{
    public const float Radius = 0.5f;   // 1 m diameter
    public const int MaxSubdivisions = 8; // 163,842 verts before seam fixes

    /// <param name="subdivisions">
    /// 0 = octahedron (8 tris), 1 = icosahedron (20 tris),
    /// 2+ = subdivided icosahedron (each level multiplies triangles by 4).
    /// </param>
    public static Mesh Create(int subdivisions)
    {
        subdivisions = Mathf.Clamp(subdivisions, 0, MaxSubdivisions);

        var dirs = new List<Vector3>();
        var tris = new List<int>();
        if (subdivisions == 0) BuildOctahedron(dirs, tris);
        else BuildIcosahedron(dirs, tris);

        // --- Subdivide ---
        var midCache = new Dictionary<long, int>();
        for (int s = 1; s < subdivisions; s++)
        {
            var next = new List<int>(tris.Count * 4);
            midCache.Clear();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = Midpoint(a, b, dirs, midCache);
                int bc = Midpoint(b, c, dirs, midCache);
                int ca = Midpoint(c, a, dirs, midCache);

                next.Add(a); next.Add(ab); next.Add(ca);
                next.Add(b); next.Add(bc); next.Add(ab);
                next.Add(c); next.Add(ca); next.Add(bc);
                next.Add(ab); next.Add(bc); next.Add(ca);
            }
            tris = next;
        }

        EnforceOutwardWinding(dirs, tris);

        // --- UVs ---
        var uvs = new List<Vector2>(dirs.Count);
        foreach (var d in dirs) uvs.Add(DirectionToUV(d));

        FixSeam(dirs, uvs, tris);
        FixPoles(dirs, uvs, tris);

        // --- Build mesh ---
        var vertices = new Vector3[dirs.Count];
        var normals = new Vector3[dirs.Count];
        for (int i = 0; i < dirs.Count; i++)
        {
            normals[i] = dirs[i];
            vertices[i] = dirs[i] * Radius;
        }

        var mesh = new Mesh { name = $"IcoSphere_{subdivisions}" };
        if (vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ------------------------------------------------------------------

    private static void BuildOctahedron(List<Vector3> v, List<int> t)
    {
        v.Add(Vector3.up);      // 0
        v.Add(Vector3.down);    // 1
        v.Add(Vector3.right);   // 2
        v.Add(Vector3.left);    // 3
        v.Add(Vector3.forward); // 4
        v.Add(Vector3.back);    // 5

        // Winding is corrected later by EnforceOutwardWinding.
        t.AddRange(new[]
        {
            0, 4, 2,   0, 2, 5,   0, 5, 3,   0, 3, 4,
            1, 2, 4,   1, 5, 2,   1, 3, 5,   1, 4, 3
        });
    }

    private static void BuildIcosahedron(List<Vector3> v, List<int> t)
    {
        float p = (1f + Mathf.Sqrt(5f)) * 0.5f;

        v.Add(new Vector3(-1, p, 0).normalized);
        v.Add(new Vector3(1, p, 0).normalized);
        v.Add(new Vector3(-1, -p, 0).normalized);
        v.Add(new Vector3(1, -p, 0).normalized);
        v.Add(new Vector3(0, -1, p).normalized);
        v.Add(new Vector3(0, 1, p).normalized);
        v.Add(new Vector3(0, -1, -p).normalized);
        v.Add(new Vector3(0, 1, -p).normalized);
        v.Add(new Vector3(p, 0, -1).normalized);
        v.Add(new Vector3(p, 0, 1).normalized);
        v.Add(new Vector3(-p, 0, -1).normalized);
        v.Add(new Vector3(-p, 0, 1).normalized);

        t.AddRange(new[]
        {
            0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
            1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
            3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
            4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
        });
    }

    private static int Midpoint(int a, int b, List<Vector3> dirs, Dictionary<long, int> cache)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (cache.TryGetValue(key, out int idx)) return idx;

        idx = dirs.Count;
        dirs.Add(((dirs[a] + dirs[b]) * 0.5f).normalized);
        cache[key] = idx;
        return idx;
    }

    /// Unity uses clockwise winding for front faces; make every triangle face outward.
    private static void EnforceOutwardWinding(List<Vector3> dirs, List<int> tris)
    {
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = dirs[tris[i]], b = dirs[tris[i + 1]], c = dirs[tris[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
            {
                (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            }
        }
    }

    /// Equirectangular mapping. U increases to the right when viewed from outside,
    /// seam lies on the -Z side, V = 0 at the bottom pole, 1 at the top.
    private static Vector2 DirectionToUV(Vector3 d)
    {
        float u = 0.5f - Mathf.Atan2(d.x, d.z) / (2f * Mathf.PI);
        float v = 0.5f + Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI;
        return new Vector2(u, v);
    }

    private static bool IsPole(Vector3 d) => Mathf.Abs(d.y) > 0.99999f;

    /// Triangles that wrap around U (e.g. 0.95 -> 0.05) get duplicated vertices with U + 1,
    /// so the texture doesn't smear backwards across the whole map.
    private static void FixSeam(List<Vector3> dirs, List<Vector2> uvs, List<int> tris)
    {
        var dupes = new Dictionary<int, int>();
        for (int i = 0; i < tris.Count; i += 3)
        {
            float min = float.MaxValue, max = float.MinValue;
            for (int k = 0; k < 3; k++)
            {
                int idx = tris[i + k];
                if (IsPole(dirs[idx])) continue;
                float u = uvs[idx].x;
                if (u < min) min = u;
                if (u > max) max = u;
            }
            if (max - min <= 0.5f) continue;

            for (int k = 0; k < 3; k++)
            {
                int idx = tris[i + k];
                if (IsPole(dirs[idx]) || uvs[idx].x >= 0.5f) continue;

                if (!dupes.TryGetValue(idx, out int dup))
                {
                    dup = dirs.Count;
                    dirs.Add(dirs[idx]);
                    uvs.Add(new Vector2(uvs[idx].x + 1f, uvs[idx].y));
                    dupes[idx] = dup;
                }
                tris[i + k] = dup;
            }
        }
    }

    /// Each triangle touching a pole gets its own pole vertex with U centered
    /// between the other two vertices — removes the pinching/swirl artifact.
    private static void FixPoles(List<Vector3> dirs, List<Vector2> uvs, List<int> tris)
    {
        for (int i = 0; i < tris.Count; i += 3)
        {
            for (int k = 0; k < 3; k++)
            {
                int idx = tris[i + k];
                if (!IsPole(dirs[idx])) continue;

                float u = (uvs[tris[i + (k + 1) % 3]].x + uvs[tris[i + (k + 2) % 3]].x) * 0.5f;
                int dup = dirs.Count;
                dirs.Add(dirs[idx]);
                uvs.Add(new Vector2(u, uvs[idx].y));
                tris[i + k] = dup;
            }
        }
    }
}