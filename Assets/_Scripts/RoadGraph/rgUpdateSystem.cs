using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial struct rgUpdateSystem : ISystem
{
    private ComponentLookup<LocalTransform> LocalTranformPositions;
    private ComponentLookup<rgEdge> Edges;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        LocalTranformPositions.Update(ref state);
        Edges.Update(ref state);

        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<rgEdge>();
        NativeArray<Entity> edgeEntities = state.EntityManager.CreateEntityQuery(entityQueryBuilder).ToEntityArray(Allocator.TempJob);

        new UpdateEdgesPositions { LocalTransformPositions = LocalTranformPositions }.ScheduleParallel();
        new UpdateNodes { Edges = Edges, EdgeEntities = edgeEntities }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct UpdateEdgesPositions : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalTransform> LocalTransformPositions;

        [BurstCompile]
        private void Execute(in rgEdge edge, ref rgEdgePosiotions edgePosiotions)
        {
            edgePosiotions.Pos1 = LocalTransformPositions[edge.Node1].Position;
            edgePosiotions.Pos2 = LocalTransformPositions[edge.Node2].Position;
        }
    }

    [BurstCompile]
    public partial struct UpdateNodes : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<rgEdge> Edges;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> EdgeEntities;

        [BurstCompile]
        private void Execute(Entity nodeEnt, ref DynamicBuffer<rgNodeEdges> nodeEdges)
        {
            nodeEdges.Clear();
            foreach(Entity ent in EdgeEntities)
            {
                var edge = Edges[ent];
                if (edge.Node1 == nodeEnt)
                    nodeEdges.Add(new rgNodeEdges { OtherNodeEnt = edge.Node2, EdgeEnt = ent });
                if (edge.Node2 == nodeEnt)
                    nodeEdges.Add(new rgNodeEdges { OtherNodeEnt = edge.Node1, EdgeEnt = ent });
            }
        }
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        LocalTranformPositions = state.GetComponentLookup<LocalTransform>(true);
        Edges = state.GetComponentLookup<rgEdge>(true);
    }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}