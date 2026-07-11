using Unity.Entities;
using Unity.Mathematics;

public struct RoadSegmentNodeEnt
{
    public Entity Ent;

    public static implicit operator Entity(RoadSegmentNodeEnt val) => val.Ent;
    public static implicit operator RoadSegmentNodeEnt(Entity val) => new() { Ent = val };
}

public struct RoadSegmentLaneEnt
{
    public Entity Ent;

    public static implicit operator Entity(RoadSegmentLaneEnt val) => val.Ent;
    public static implicit operator RoadSegmentLaneEnt(Entity val) => new() { Ent = val };
}
#region Road Lane Node Entity

public struct RoadLaneNodeData : IComponentData
{
    public float4x4 Transform;

    public Entity ParentSegmentNodeEnt;


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

public struct RoadLaneUpdateMesh : IComponentData, IEnableableComponent
{
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
    public Entity Pivot;
}

public struct RoadSegmentNodeElements : IBufferElementData
{
    public Entity RoadLaneNodeEnt;
}

public struct RoadSegmentNodeData : IComponentData
{
    public Line2D Line;
}

#endregion Road Segment Node Entity

public class rgSpawnRoadDataFromJsonText : IComponentData
{
    public string JsonText;
}

public struct RoadVisualizerParent : IComponentData
{
    public Entity ParentEnt;
}
