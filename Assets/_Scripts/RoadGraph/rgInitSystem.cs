using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(rgUpdateSystem))]
[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial struct rgInitSystem : ISystem
{
    Entity SpawnNode(EntityManager manager, Entity prefab, float3 position, Entity RoadManagerEnt)
    {
        var node = manager.Instantiate(prefab);
        manager.SetComponentData(node, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = 1 });
        manager.AddBuffer<rgNodeEdges>(node);

        manager.GetBuffer<rgRoadNodes>(RoadManagerEnt).Add(new rgRoadNodes { Node = node });
        return node;
    }

    public Entity SpawnEdge(EntityManager manager, Entity Node1, Entity Node2, Entity RoadManagerEnt)
    {
        NativeList<ComponentType> types = new(2, Allocator.Temp)
        {
            ComponentType.ReadOnly<rgEdge>(),
            ComponentType.ReadOnly<rgEdgePosiotions>(),
            ComponentType.ReadOnly<SceneSection>(),
            ComponentType.ReadOnly<SceneTag>()
        };

        var arch = manager.CreateArchetype(types.AsArray());
        var edge = manager.CreateEntity(arch);
        manager.SetComponentData(edge, new rgEdge { Node1 = Node1, Node2 = Node2 });
        manager.SetName(edge, "rgEdge");
        SceneSection sceneSection = manager.GetSharedComponent<SceneSection>(RoadManagerEnt);
        SceneTag sceneTag = manager.GetSharedComponent<SceneTag>(RoadManagerEnt);
        manager.SetSharedComponent(edge, sceneSection);
        manager.SetSharedComponent(edge, sceneTag);

        manager.GetBuffer<rgRoadEdges>(RoadManagerEnt).Add(new rgRoadEdges { Edge = edge });
        return edge;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;
        state.Enabled = false;


        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentC>());
        var manager = state.EntityManager;

        Entity nodePrefab = Document.NodePrefab;
        Entity roadManagerEnt = Document.RoadManagerEnt;
        var Node1 = SpawnNode(manager, nodePrefab, new float3(-35, 0, -30), roadManagerEnt);
        var Node2 = SpawnNode(manager, nodePrefab, new float3(-32, 0, 35), roadManagerEnt);
        SpawnEdge(manager, Node1, Node2, roadManagerEnt);

        var Node3 = SpawnNode(manager, nodePrefab, new float3(25, 0, -31), roadManagerEnt);
        var Node4 = SpawnNode(manager, nodePrefab, new float3(27, 0, 34), roadManagerEnt);
        SpawnEdge(manager, Node3, Node4, roadManagerEnt);

     
        SpawnEdge(manager, Node2, Node3, roadManagerEnt);
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    { }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}