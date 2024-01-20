using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DrawShapesSystem : SystemBase
{
    private Mesh SphereMesh;
    private Mesh CubeLineMesh;
    private Dictionary<Color, RenderParams> m_RenderParams = new();

    [BurstDiscard]
    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;

        DrawRoadMapEdges(Color.cyan);
        DrawRoadDirectionArrows(Color.white);
        DrawVehiclePath(Color.magenta);
        DrawVehicleBoxes(Color.blue);

        //DrawVehiclePathIntercestions();
    }

    private void DrawVehiclePathIntercestions()
    {
        var rpShpere1 = GetRenderParams(Color.red);
        var rpShpere2 = GetRenderParams(new Color(1, 0.6f, 0.6f));
        var rpLines = GetRenderParams(Color.yellow);
        NativeList<VehicleAspect> vehicles = new NativeList<VehicleAspect>(Allocator.Temp);

        Entities.WithoutBurst().ForEach((VehicleAspect vehicle) =>
        {
            vehicles.Add(vehicle);
        }).Run();

        Entities.WithoutBurst().ForEach((VehicleAspect vehicle) =>
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicle.Entity == vehicles[i].Entity)
                    continue;

                var intersect = vehicle.GetPathIntersection(vehicles[i], 1000, 5);
                DrawSphere(rpShpere1, intersect.MyPosition, 0.8f);
                DrawSphere(rpShpere2, intersect.OtherPosition, 0.8f);
                DrawLine(rpLines, intersect.MyPosition, intersect.OtherPosition, new float2(1, 1));
            }
        }).Run();
    }

    private void DrawVehicleBoxes(Color color)
    {
        RenderParams rp = GetRenderParams(color);

        Entities.WithoutBurst().ForEach((VehicleAspect vehicle) =>
        {
            var obb = vehicle.GetObb();
            Graphics.RenderMesh(rp, CubeLineMesh, 0, obb.GetMatrix());

        }).Run();
    }

    private void DrawRoadMapEdges(Color color)
    {
        RenderParams rp = GetRenderParams(color);
        Entities.WithoutBurst().ForEach((in rgEdgePosiotions edge) =>
        {
            DrawLine(rp, edge.Pos1, edge.Pos2, new float2(1, 1));
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
                DrawLine(rp, p1, p2, new float2(0.4f, 1.6f));
            }
        }).Run();
    }

    private void DrawLine(RenderParams rp, float3 p1, float3 p2, float2 size)
    {
        if ((p1 - p2).lengthsq() < 0.01f)
            return;
        float3 center = (p1 + p2) / 2;
        Quaternion quaternion = Quaternion.LookRotation(p1 - p2);

        float3 scale = new() { x = size.x, y = size.y, z = (p1 - p2).length() };
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(center, quaternion, scale);
        Graphics.RenderMesh(rp, SphereMesh, 0, matrix4X4);
    }
    private void DrawSphere(RenderParams rp, float3 pos, float3 size)
    {
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(pos, quaternion.identity, size);
        Graphics.RenderMesh(rp, SphereMesh, 0, matrix4X4);
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