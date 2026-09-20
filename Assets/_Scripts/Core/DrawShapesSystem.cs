using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DrawShapesSystem : SystemBase
{
    private Mesh SphereMesh;
    private Mesh CubeLineMesh;
    private Dictionary<Color, RenderParams> m_RenderParams = new();

    protected override void OnUpdate()
    {
    }

    public void DrawLine(RenderParams rp, float3 p1, float3 p2, float2 size)
    {
        if ((p1 - p2).lengthsq() < 0.01f)
            return;
        float3 center = (p1 + p2) / 2;
        Quaternion quaternion = Quaternion.LookRotation(p1 - p2);

        float3 scale = new() { x = size.x, y = size.y, z = (p1 - p2).length() };
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(center, quaternion, scale);
        Graphics.RenderMesh(rp, SphereMesh, 0, matrix4X4);
    }

    public void DrawSphere(Color color, float3 pos, float3 size)
    {
        DrawSphere(GetRenderParams(color), pos, size);
    }

    public void DrawSphere(RenderParams rp, float3 pos, float3 size)
    {
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(pos, quaternion.identity, size);
        Graphics.RenderMesh(rp, SphereMesh, 0, matrix4X4);
    }

    public Color RandomColor(int seed)
    {
        return Color.HSVToRGB(seed * 1.61803398875f, 1, 1);
    }

    protected override void OnCreate()
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SphereMesh = GameObject.Instantiate(obj.GetComponent<MeshFilter>().mesh);
        GameObject.Destroy(obj);

        CubeLineMesh = new Mesh();
        float x = 0.5f;
        List<Vector3> verts = new()
        {
            new Vector3(-x,-x,x),
            new Vector3(x,-x,x),
            new Vector3(x,x,x),
            new Vector3(-x,x,x),
            new Vector3(-x,-x,-x),
            new Vector3(x,-x,-x),
            new Vector3(x,x,-x),
            new Vector3(-x,x,-x),
        };
        CubeLineMesh.SetVertices(verts);
        List<int> indices = new()
        {
            0,1,1,2,2,3,3,0,4,5,5,6,6,7,7,4,0,4,1,5,2,6,3,7
        };
        CubeLineMesh.SetIndices(indices, MeshTopology.Lines, 0);
    }

    public RenderParams GetRenderParams(Color color)
    {
        if (m_RenderParams.TryGetValue(color, out var material))
            return material;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material newMat = new Material(lit);

        newMat.color = color;
        RenderParams rp = new RenderParams(newMat);
        m_RenderParams[color] = rp;

        return rp;
    }
}