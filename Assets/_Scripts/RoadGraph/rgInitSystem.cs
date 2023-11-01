using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct rgInitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;
        state.Enabled = false;


        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentC>());
        var manager = state.EntityManager;

        var Node1 = Document.SpawnNode(ref manager, new float3(-15, 0, -10));
        var Node2 = Document.SpawnNode(ref manager, new float3(-15, 0, 15));
        Document.SpawnEdge(ref manager, Node1, Node2);

        var Node3 = Document.SpawnNode(ref manager, new float3(15, 0, -11));
        var Node4 = Document.SpawnNode(ref manager, new float3(15, 0, 14));
        Document.SpawnEdge(ref manager, Node3, Node4);

     
        Document.SpawnEdge(ref manager, Node2, Node3);
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    { }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}