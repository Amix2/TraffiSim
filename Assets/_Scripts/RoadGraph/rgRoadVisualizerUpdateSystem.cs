using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct RoadLaneNodeVisualizerUpdateJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<RoadLaneNodeData> RoadLaneNodeDataLookup;

    public void Execute(ref LocalTransform transform, in RoadLaneNodeVisualizer roadLaneNodeVisualizer)
    {
        Entity AttachedRoadLane = roadLaneNodeVisualizer.RoadLaneNodeEnt;
        if (RoadLaneNodeDataLookup.HasComponent(AttachedRoadLane))
        {
            RoadLaneNodeData roadLaneNodeData = RoadLaneNodeDataLookup[AttachedRoadLane];
            transform = LocalTransform.FromMatrix(roadLaneNodeData.Transform);
        }
    }
}

[BurstCompile]
public partial struct RoadLaneVisualizerUpdateJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<RoadLaneNodeData> RoadLaneNodeDataLookup;
    [ReadOnly] public ComponentLookup<RoadLaneData> RoadLaneDataLookup;

    public void Execute(ref LocalTransform transform, in RoadLaneVisualizer roadLaneNodeVisualizer)
    {
        Entity AttachedRoadLane = roadLaneNodeVisualizer.RoadLaneEnt;
        if (RoadLaneDataLookup.HasComponent(AttachedRoadLane))
        {
            RoadLaneData roadLaneData = RoadLaneDataLookup[AttachedRoadLane];

            float3 startPos = float3.zero;
            if (RoadLaneNodeDataLookup.TryGetComponent(roadLaneData.StartNodeEnt, out RoadLaneNodeData startNodeData))
                startPos = startNodeData.Position;

            float3 endPos = float3.zero;
            if (RoadLaneNodeDataLookup.TryGetComponent(roadLaneData.EndNodeEnt, out RoadLaneNodeData endNodeData))
                endPos = endNodeData.Position;

            float3 dir = endPos - startPos;
            float dist = math.length(dir);

            float3 position = (startPos + endPos) * 0.5f;
            quaternion rotation = quaternion.LookRotationSafe(dir, math.up());
            float4x4 m = float4x4.TRS(position, rotation, dist);   // uniform scale

            transform = LocalTransform.FromMatrix(m);
        }
    }
}

public partial struct RoadVisualizerUpdate : ISystem
{
    private ComponentLookup<RoadLaneNodeData> RoadLaneNodeDataLookup;
    private ComponentLookup<RoadLaneData> RoadLaneDataLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RoadLaneNodeDataLookup = state.GetComponentLookup<RoadLaneNodeData>(true);
        RoadLaneDataLookup = state.GetComponentLookup<RoadLaneData>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RoadLaneNodeDataLookup.Update(ref state);
        RoadLaneDataLookup.Update(ref state);
        new RoadLaneNodeVisualizerUpdateJob { RoadLaneNodeDataLookup = RoadLaneNodeDataLookup }.ScheduleParallel();
        new RoadLaneVisualizerUpdateJob { RoadLaneNodeDataLookup = RoadLaneNodeDataLookup, RoadLaneDataLookup = RoadLaneDataLookup }.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}