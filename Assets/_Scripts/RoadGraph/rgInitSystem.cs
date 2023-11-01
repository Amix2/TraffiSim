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

        var Node1 = Document.SpawnNode(ref manager, new float3(-35, 0, -30));
        var Node2 = Document.SpawnNode(ref manager, new float3(-32, 0, 35));
        Document.SpawnEdge(ref manager, Node1, Node2);

        var Node3 = Document.SpawnNode(ref manager, new float3(35, 0, -31));
        var Node4 = Document.SpawnNode(ref manager, new float3(37, 0, 34));
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