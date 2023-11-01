using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DrawShapesSystem : SystemBase
{
    Mesh mesh;
    Dictionary<Color, RenderParams> m_RenderParams = new();

    protected override void OnUpdate()
    {
        DrawRoadMapEdges(Color.cyan);
        DrawVehiclePath(Color.magenta);
    }

    private void DrawRoadMapEdges(Color color)
    {
        RenderParams rp = GetRenderParams(color);
        Entities.WithoutBurst().ForEach((in rgEdgePosiotions edge) =>
        {
            float3 center = (edge.Pos1 + edge.Pos2) / 2;
            Quaternion quaternion = math.any(edge.Pos1 != edge.Pos2) ? Quaternion.LookRotation(edge.Pos1 - edge.Pos2) : Quaternion.identity;
            float3 scale = new() { x = 1, y = 1, z = (edge.Pos1 - edge.Pos2).length() };
            Matrix4x4 matrix4X4 = Matrix4x4.TRS(center, quaternion, scale);
            Graphics.RenderMesh(rp, mesh, 0, matrix4X4);

        }).Run();
    }

    private void DrawVehiclePath(Color color)
    {
        RenderParams rp = GetRenderParams(color);
        Entities.WithoutBurst().ForEach((in LocalToWorld localToWorld, in DynamicBuffer<PathBuffer> paths) =>
        {
            for(int i=0; i<paths.Length; i++) 
            {
                float3 p1 = paths[i].Position;
                float3 p2 = i > 0 ? paths[i-1].Position : localToWorld.Position;
                float3 center = (p1 + p2) / 2;
                Quaternion quaternion = math.any(p1 != p2) ? Quaternion.LookRotation(p1 - p2) : Quaternion.identity;

                float3 scale = new() { x = .4f, y = 1.6f, z = (p1- p2).length() };
                Matrix4x4 matrix4X4 = Matrix4x4.TRS(center, quaternion, scale);
                Graphics.RenderMesh(rp, mesh, 0, matrix4X4);
            }
        }).Run();
    }

    protected override void OnCreate()
    {

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mesh = GameObject.Instantiate(obj.GetComponent<MeshFilter>().mesh);
        GameObject.Destroy(obj);


    }

    RenderParams GetRenderParams(Color color)
    {
        if(m_RenderParams.TryGetValue(color, out var material))
            return material;

        //const string DefaultShader = "Universal Render Pipeline/Lit";
        //UnityEngine.Assertions.Assert.IsNotNull(Shader.Find(DefaultShader));
        var newMat = GameObject.Instantiate(GraphicsSettings.defaultRenderPipeline.defaultMaterial);
        newMat.color = color;
        RenderParams rp = new RenderParams(newMat);

        m_RenderParams[color] = rp;
        return rp;
    }
}