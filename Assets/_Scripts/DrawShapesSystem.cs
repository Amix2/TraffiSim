using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DrawShapesSystem : SystemBase
{
    private Mesh SphereMesh;
    private Dictionary<Color, RenderParams> m_RenderParams = new();

    [BurstDiscard]
    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;

        DrawRoadMapEdges(Color.cyan);
        DrawRoadDirectionArrows(Color.white);
        DrawVehiclePath(Color.magenta);
    }

    private void DrawRoadMapEdges(Color color)
    {
        RenderParams rp = GetRenderParams(color);
        Entities.WithoutBurst().ForEach((in rgEdgePosiotions edge) =>
        {
            if ((edge.Pos1 - edge.Pos2).lengthsq() < 0.01f)
                return;
            float3 center = (edge.Pos1 + edge.Pos2) / 2;
            Quaternion quaternion = Quaternion.LookRotation(edge.Pos1 - edge.Pos2);
            float3 scale = new() { x = 1, y = 1, z = (edge.Pos1 - edge.Pos2).length() };
            Matrix4x4 matrix4X4 = Matrix4x4.TRS(center, quaternion, scale);
            Graphics.RenderMesh(rp, SphereMesh, 0, matrix4X4);
        }).Run();
    }

    private void DrawRoadDirectionArrows(Color color)
    {
        var ArrowMesh = EntityManager.GetSharedComponentManaged<DocumentSharedComponent>(SystemAPI.GetSingletonEntity<DocumentComponent>()).ArrowMesh;
        RenderParams rp = GetRenderParams(color);
        Entities.WithoutBurst().ForEach((in rgEdgePosiotions edge) =>
        {
            if ((edge.Pos1 - edge.Pos2).lengthsq() < 0.01f)
                return;
            float3 center = (edge.Pos1 + edge.Pos2) / 2;
            Quaternion quaternion = Quaternion.LookRotation(edge.Pos2 - edge.Pos1);
            float3 up = quaternion * new Vector3(0, 1, 0);
            float3 scale = new() { x = 1, y = 1, z = 1 };
            Matrix4x4 matrix4X4 = Matrix4x4.TRS(center + up * 2, quaternion, scale);
            Graphics.RenderMesh(rp, ArrowMesh, 0, matrix4X4);
        }).Run();
    }

    private void DrawVehiclePath(Color color)
    {
        RenderParams rp = GetRenderParams(color);
        Entities.WithoutBurst().ForEach((in LocalToWorld localToWorld, in DynamicBuffer<PathBuffer> paths) =>
        {
            for (int i = 0; i < paths.Length; i++)
            {
                float3 p1 = paths[i].Position;
                float3 p2 = i > 0 ? paths[i - 1].Position : localToWorld.Position;
                if ((p1 - p2).lengthsq() < 0.01f)
                    continue;
                float3 center = (p1 + p2) / 2;
                Quaternion quaternion = Quaternion.LookRotation(p1 - p2);

                float3 scale = new() { x = .4f, y = 1.6f, z = (p1 - p2).length() };
                Matrix4x4 matrix4X4 = Matrix4x4.TRS(center, quaternion, scale);
                Graphics.RenderMesh(rp, SphereMesh, 0, matrix4X4);
            }
        }).Run();
    }

    protected override void OnCreate()
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SphereMesh = GameObject.Instantiate(obj.GetComponent<MeshFilter>().mesh);
        GameObject.Destroy(obj);
    }

    private RenderParams GetRenderParams(Color color)
    {
        if (m_RenderParams.TryGetValue(color, out var material))
            return material;

        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return new RenderParams();  // just some trash

        var Document = SystemAPI.GetAspect<DocumentAspect>(SystemAPI.GetSingletonEntity<DocumentComponent>());
        var defaultShader = EntityManager.GetSharedComponentManaged<DocumentSharedComponent>(Document.Entity).DefaultShader;

        Material newMat = new Material(defaultShader);

        newMat.color = color;
        RenderParams rp = new RenderParams(newMat);
        m_RenderParams[color] = rp;

        return rp;
    }
}