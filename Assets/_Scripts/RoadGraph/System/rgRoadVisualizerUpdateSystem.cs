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
    [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<MaterialPropertyTextureTiling> MaterialPropertyTextureTilingLookup;

    public void Execute(in RoadLaneVisualizerData RoadLaneVisualizerData, in RoadLaneData roadLaneData)
    {
        float3 startPos = float3.zero;
        if (RoadLaneNodeDataLookup.TryGetComponent(roadLaneData.StartNodeEnt, out RoadLaneNodeData startNodeData))
            startPos = startNodeData.Position;

        float3 endPos = float3.zero;
        if (RoadLaneNodeDataLookup.TryGetComponent(roadLaneData.EndNodeEnt, out RoadLaneNodeData endNodeData))
            endPos = endNodeData.Position;

        float3 dir = endPos - startPos;
        float dist = math.length(dir);
        float LaneWidth = roadLaneData.LaneWidth;

        float3 position = (startPos + endPos) * 0.5f;
        quaternion rotation = dir.MakeXDirection();
        LocalTransformLookup[RoadLaneVisualizerData.VisualizerEnt] = LocalTransform.FromPositionRotation(position, rotation);

        PostTransformMatrixLookup[RoadLaneVisualizerData.VisualizerEnt] = new PostTransformMatrix { Value = float4x4.Scale(dist, 1, LaneWidth) };

        float LengthPerTexTile = LaneWidth * 2;
        float TexTile = math.round(dist / LengthPerTexTile);
        TexTile = math.max(TexTile, 1);
        MaterialPropertyTextureTiling textureTiling = MaterialPropertyTextureTilingLookup[RoadLaneVisualizerData.MarkingsEnt];
        textureTiling.Value.x = TexTile;
        textureTiling.Value.y = 1;
        MaterialPropertyTextureTilingLookup[RoadLaneVisualizerData.MarkingsEnt] = textureTiling;
    }
}

public partial struct RoadVisualizerUpdate : ISystem
{
    private ComponentLookup<RoadLaneNodeData> RoadLaneNodeDataLookup;
    private ComponentLookup<RoadLaneData> RoadLaneDataLookup;
    private ComponentLookup<LocalTransform> LocalTransformLookup;
    private ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
    private ComponentLookup<MaterialPropertyTextureTiling> MaterialPropertyTextureTilingLookup;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RoadLaneNodeDataLookup = state.GetComponentLookup<RoadLaneNodeData>(true);
        RoadLaneDataLookup = state.GetComponentLookup<RoadLaneData>(true);
        LocalTransformLookup = state.GetComponentLookup<LocalTransform>(false);
        PostTransformMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(false);
        MaterialPropertyTextureTilingLookup = state.GetComponentLookup<MaterialPropertyTextureTiling>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RoadLaneNodeDataLookup.Update(ref state);
        RoadLaneDataLookup.Update(ref state);
        LocalTransformLookup.Update(ref state);
        PostTransformMatrixLookup.Update(ref state);
        MaterialPropertyTextureTilingLookup.Update(ref state);

        state.Dependency = new RoadLaneNodeVisualizerUpdateJob { RoadLaneNodeDataLookup = RoadLaneNodeDataLookup }.ScheduleParallel(state.Dependency);

        state.Dependency =
        new RoadLaneVisualizerUpdateJob
        {
            RoadLaneNodeDataLookup = RoadLaneNodeDataLookup,
            RoadLaneDataLookup = RoadLaneDataLookup,
            LocalTransformLookup = LocalTransformLookup,
            PostTransformMatrixLookup = PostTransformMatrixLookup,
            MaterialPropertyTextureTilingLookup = MaterialPropertyTextureTilingLookup,
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}