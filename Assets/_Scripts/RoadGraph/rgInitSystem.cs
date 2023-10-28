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
        if (!SystemAPI.HasSingleton<rgDocumentAspect>())
            return;
        state.Enabled = false;


        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentAspect>());
        var nodePrefab = Document.NodePrefab;
        var manager = state.EntityManager;

        {
            var Node1 = Document.SpawnNode(ref manager, new float3(-10, 0, -10));
            var Node2 = Document.SpawnNode(ref manager, new float3(10, 0, 10));

            Document.SpawnEdge(ref manager, Node1, Node2);
        }
        {
            var Node1 = Document.SpawnNode(ref manager, new float3(-10, 0, -10));
            var Node2 = Document.SpawnNode(ref manager, new float3(10, 0, 10));

            Document.SpawnEdge(ref manager, Node1, Node2);
        }
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    { }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}