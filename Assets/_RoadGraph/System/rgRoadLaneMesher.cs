using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

//[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class rgRoadLaneMesher : SystemBase
{
    private EntitiesGraphicsSystem EntitiesGraphicsSystem;
    private EntityQuery RoadSegmentsToUpdate;

    protected override void OnCreate()
    {
        EntitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        RoadSegmentsToUpdate = SystemAPI.QueryBuilder()
            .WithAll<RoadSegmentData, LinkedEntityGroup, RoadSegmentLane>()          // IComponentData
            .Build();
        //RequireForUpdate<RoadLaneUpdateMesh>();
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
        var Document = SystemAPI.GetSingleton<rgDocumentC>();

        var entities = RoadSegmentsToUpdate.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities)
        {
            {
                // cleanup old visualziers
                DynamicBuffer<LinkedEntityGroup> linkedEntityGroups = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);

                var ecb = new EntityCommandBuffer(Allocator.Temp);
                for (int i = 0; i < linkedEntityGroups.Length; i++)
                {
                    if (EntityManager.HasComponent<RoadVisualizerData>(linkedEntityGroups[i].Value))
                    {
                        ecb.DestroyEntity(linkedEntityGroups[i].Value);
                        linkedEntityGroups.RemoveAt(i);
                        i--;
                    }
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            {   // add new visualizers
                DynamicBuffer<LinkedEntityGroup> linkedEntityGroups = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
                DynamicBuffer<RoadSegmentLane> segmentLanes = SystemAPI.GetBuffer<RoadSegmentLane>(entity);
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                for (int i = 0; i < segmentLanes.Length; i++)
                {
                    Entity RoadSurface = EntityManager.Instantiate(Document.RoadSurfacePrefab);
                    ecb.AddComponent<RuntimeUpdateableMesh>(RoadSurface);
                    linkedEntityGroups.Add(RoadSurface);
                    //RoadVisualizerAspect roadVisualizer = new RoadVisualizerAspect(this, RoadSurface);
                    //roadVisualizer.Parent = entity;
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
            NativeList<RoadVisualizerAspect> roadVisualizers = new NativeList<RoadVisualizerAspect>(SystemAPI.GetBuffer<RoadSegmentLane>(entity).Length, Allocator.Temp);

            {
                DynamicBuffer<LinkedEntityGroup> linkedEntityGroups = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
                for (int i = 0; i < linkedEntityGroups.Length; i++)
                {
                    if (EntityManager.HasComponent<RoadVisualizerData>(linkedEntityGroups[i].Value))
                    {
                        RoadVisualizerAspect aspect = new RoadVisualizerAspect(this, linkedEntityGroups[i].Value);
                        aspect.Parent = entity;
                        roadVisualizers.Add(aspect);
                    }
                }
            }

            foreach (RoadVisualizerAspect visualizer in roadVisualizers)
            {
                Mesh mesh = BuildLaneMesh();
                visualizer.SetMesh(mesh, EntitiesGraphicsSystem);
            }

            roadVisualizers.Dispose();
        }
    }

    private Mesh BuildLaneMesh()
    {
        var mesh = new Mesh();

        var verts = new NativeList<float3>(Allocator.Temp);
        verts.Add(new float3(0, 0, 1));
        verts.Add(new float3(0, 0, 2));
        verts.Add(new float3(2, 0, 1));
        verts.Add(new float3(1, 0, 0));
        var UVs = new NativeList<float2>(Allocator.Temp);
        UVs.Add(new float2(0, 0));
        UVs.Add(new float2(0, 1));
        UVs.Add(new float2(1, 1));
        UVs.Add(new float2(1, 0));

        mesh.SetVertices(verts.AsArray());
        mesh.SetUVs(0, UVs.AsArray());
        mesh.SetIndices(new[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        verts.Dispose();
        UVs.Dispose();

        return mesh;
    }

    private RoadEdge CalculateLaneEdge(in RoadLaneData data)
    {
        RoadEdge edge = new();
        edge.Left = new NativeList<float3>(Allocator.Temp);
        edge.Right = new NativeList<float3>(Allocator.Temp);

        //float3 startPos = EntityManager.GetComponentData<RoadPortData>(data.StartPortEnt).Position;
        //float3 endPos = EntityManager.GetComponentData<RoadPortData>(data.EndPortEnt).Position;

        //float3 dir = endPos - startPos;
        //float dist = math.length(dir);

        //float3 position = (startPos + endPos) * 0.5f;
        //quaternion rotation = dir.MakeXDirection();
        //edge.localTransform = LocalTransform.FromPositionRotation(position, rotation);

        //float LengthPerTexTile = data.LaneWidth * 2;
        //float TexTile = math.round(dist / LengthPerTexTile);
        //TexTile = math.max(TexTile, 1);
        //edge.TextureTilingX = TexTile;

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
        {
            ecb.AddComponent(parent, comp);
            ecb.AddComponent<RuntimeUpdateableMeshAliveTag>(parent);
        }

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
            //comp.MatID = EntitiesGraphicsSystem.RegisterMaterial(rma.GetMaterial(bakedMmi));
        }

        //EntityManager.SetComponentData(child, new MaterialMeshInfo(comp.MatID, meshID));
        EntityManager.SetComponentData(child, bounds);

        comp.Id = meshID;     // mirror for reference; Mesh stays null -> cleanup skips it
                              // comp.Mesh intentionally left null: child is a borrower, frees nothing.
        if (EntityManager.HasComponent<MaterialPropertyTextureTiling>(child))
        {
            MaterialPropertyTextureTiling textureTiling = new MaterialPropertyTextureTiling();
            textureTiling.Value.x = 1;
            textureTiling.Value.y = 1;
            EntityManager.SetComponentData(child, textureTiling);
        }

        if (firstBuild)
        {
            ecb.AddComponent(child, comp);
            ecb.AddComponent<RuntimeUpdateableMeshAliveTag>(child);
        }
    }
}