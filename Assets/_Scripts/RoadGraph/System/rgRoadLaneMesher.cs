using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class rgRoadLaneMesher : SystemBase
{
    private EntitiesGraphicsSystem EntitiesGraphicsSystem;

    protected override void OnCreate()
    {
        EntitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        RequireForUpdate<RoadLaneUpdateMesh>();
    }

    private struct RoadEdge
    {
        public NativeList<float3> Left;
        public NativeList<float3> Right;
        public float CenterLength;
        public LocalTransform localTransform;
        public float TextureTilingX;
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (vis, roadLaneData, entity) in
                 SystemAPI.Query<RefRO<RoadLaneVisualizerData>, RefRO<RoadLaneData>>()
                          .WithAll<RoadLaneUpdateMesh>()
                          .WithEntityAccess())
        {
            Entity parent = vis.ValueRO.VisualizerEnt;          // owns the mesh
            Entity bg = vis.ValueRO.BackgroundEnt;
            Entity mk = vis.ValueRO.MarkingsEnt;

            var mesh = BuildLaneMesh(roadLaneData.ValueRO);

            // 1. Parent owns lifetime: free previous, register new, once.
            var meshID = UpdateOwnerMesh(parent, mesh, ecb);

            // 2. Children just render that shared id with their own material.
            var bounds = new RenderBounds { Value = mesh.bounds.ToAABB() };
            BindRenderChild(bg, meshID, bounds, ecb);
            BindRenderChild(mk, meshID, bounds, ecb);

            SystemAPI.SetComponentEnabled<RoadLaneUpdateMesh>(entity, false);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private Mesh BuildLaneMesh(in RoadLaneData data)
    {
        var mesh = new Mesh();

        var edge = CalculateLaneEdge(data);
        edge.Left.Dispose();    // these Temp lists were leaking before
        edge.Right.Dispose();

        var verts = new NativeList<float3>(Allocator.Temp);
        verts.Add(new float3(0, 0, 0));
        verts.Add(new float3(10, 10, 10));
        verts.Add(new float3(10, 0, 0));
        mesh.SetVertices(verts.AsArray());
        mesh.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        verts.Dispose();

        return mesh;
    }

    private RoadEdge CalculateLaneEdge(in RoadLaneData data)
    {
        RoadEdge edge = new();
        edge.Left = new NativeList<float3>(Allocator.Temp);
        edge.Right = new NativeList<float3>(Allocator.Temp);

        float3 startPos = EntityManager.GetComponentData<RoadLaneNodeData>(data.StartNodeEnt).Position;
        float3 endPos = EntityManager.GetComponentData<RoadLaneNodeData>(data.EndNodeEnt).Position;

        float3 dir = endPos - startPos;
        float dist = math.length(dir);

        float3 position = (startPos + endPos) * 0.5f;
        quaternion rotation = dir.MakeXDirection();
        edge.localTransform = LocalTransform.FromPositionRotation(position, rotation);

        float LengthPerTexTile = data.LaneWidth * 2;
        float TexTile = math.round(dist / LengthPerTexTile);
        TexTile = math.max(TexTile, 1);
        edge.TextureTilingX = TexTile;

        return edge;
    }

    // Parent: sole owner. Holds Mesh + Id, frees the old one on rebuild.
    private BatchMeshID UpdateOwnerMesh(Entity parent, Mesh mesh, EntityCommandBuffer ecb)
    {
        bool firstBuild = !EntityManager.HasComponent<RuntimeUpdateableMesh>(parent);
        var comp = firstBuild
            ? new RuntimeUpdateableMesh()
            : EntityManager.GetComponentObject<RuntimeUpdateableMesh>(parent);

        if (!firstBuild && comp.Mesh != null)
        {
            EntitiesGraphicsSystem.UnregisterMesh(comp.Id);
            UnityEngine.Object.Destroy(comp.Mesh);
        }

        var meshID = EntitiesGraphicsSystem.RegisterMesh(mesh);
        comp.Mesh = mesh;     // parent is the only one with a non-null Mesh -> frees exactly once
        comp.Id = meshID;
        // comp.MatID unused on the parent — it doesn't render.

        if (firstBuild)
            ecb.AddComponent(parent, comp);

        return meshID;
    }

    // Child: renders the shared mesh id with its own material. Never owns/frees the mesh.
    private void BindRenderChild(Entity child, BatchMeshID meshID, RenderBounds bounds,
                                 EntityCommandBuffer ecb)
    {
        bool firstBuild = !EntityManager.HasComponent<RuntimeUpdateableMesh>(child);
        var comp = firstBuild
            ? new RuntimeUpdateableMesh()
            : EntityManager.GetComponentObject<RuntimeUpdateableMesh>(child);

        if (firstBuild)
        {
            var bakedMmi = EntityManager.GetComponentData<MaterialMeshInfo>(child);
            var rma = EntityManager.GetSharedComponentManaged<RenderMeshArray>(child);
            comp.MatID = EntitiesGraphicsSystem.RegisterMaterial(rma.GetMaterial(bakedMmi));
        }

        EntityManager.SetComponentData(child, new MaterialMeshInfo(comp.MatID, meshID));
        EntityManager.SetComponentData(child, bounds);

        comp.Id = meshID;     // mirror for reference; Mesh stays null -> cleanup skips it
                              // comp.Mesh intentionally left null: child is a borrower, frees nothing.

        if (firstBuild)
            ecb.AddComponent(child, comp);
    }

   
}