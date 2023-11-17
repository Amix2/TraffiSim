using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateBefore(typeof(rgUpdateSystem))]
[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial struct rgInitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;
        state.Enabled = false;
        return;

        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentC>());
        var manager = state.EntityManager;

        Entity nodePrefab = Document.NodePrefab;
        Entity roadManagerEnt = Document.RoadManagerEnt;
        var Node1 = rgHelper.SpawnNode(manager, nodePrefab, new float3(-35, 0, -30), roadManagerEnt);
        var Node2 = rgHelper.SpawnNode(manager, nodePrefab, new float3(-32, 0, 35), roadManagerEnt);
        rgHelper.SpawnEdge(manager, Node1, Node2, roadManagerEnt);

        var Node3 = rgHelper.SpawnNode(manager, nodePrefab, new float3(25, 0, -31), roadManagerEnt);
        var Node4 = rgHelper.SpawnNode(manager, nodePrefab, new float3(27, 0, 34), roadManagerEnt);
        rgHelper.SpawnEdge(manager, Node3, Node4, roadManagerEnt);

        rgHelper.SpawnEdge(manager, Node2, Node3, roadManagerEnt);
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    { }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}