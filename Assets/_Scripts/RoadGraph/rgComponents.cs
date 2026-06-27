using Unity.Entities;
using Unity.Mathematics;

#region Road Lane Node Entity

public struct RoadLaneNodeData : IComponentData
{
    public float4x4 Transform;

    public float3 Right => new float3(Transform.c0.x, Transform.c0.y, Transform.c0.z);
    public float3 Up => new float3(Transform.c1.x, Transform.c1.y, Transform.c1.z);
    public float3 Forward => new float3(Transform.c2.x, Transform.c2.y, Transform.c2.z);
    public float3 Position => new float3(Transform.c3.x, Transform.c3.y, Transform.c3.z);
    public quaternion Rotation => new quaternion(math.orthonormalize(new float3x3(Transform)));
}

public struct RoadLaneNodeInput : IBufferElementData
{
    public Entity RoadLaneEnt;
}

public struct RoadLaneNodeOutput : IBufferElementData
{
    public Entity RoadLaneEnt;
}

public struct RoadLaneNodeUpdateInOutBuffers : IComponentData, IEnableableComponent
{
}

public struct RoadLaneNodeVisualizer : IComponentData
{
    public Entity RoadLaneNodeEnt;
}

#endregion Road Lane Node Entity

#region Road Lane Entity

public struct RoadLaneData : IComponentData
{
    public Entity StartNodeEnt, EndNodeEnt;
    public Entity Parent;
    public float LaneWidth;
}

public struct RoadLaneVisualizerData : IComponentData
{
    public Entity VisualizerEnt;
    public Entity MarkingsEnt;
    public Entity BackgroundEnt;
}

public struct RoadLaneVisualizer : IComponentData
{
    public Entity RoadLaneEnt;
}

#endregion Road Lane Entity

#region Road Segment Entity

public struct RoadSegmentPart : IBufferElementData
{
    public Entity RoadLaneEnt;
}

#endregion Road Segment Entity

#region Road Segment Node Entity

public struct RoadSegmentNodeUpdateChildNodes : IComponentData, IEnableableComponent
{
}

public struct RoadSegmentNodeElements : IBufferElementData
{
    public Entity RoadNodeEnt;
}

#endregion Road Segment Node Entity

public class rgSpawnRoadDataFromJsonText : IComponentData
{
    public string JsonText;
}