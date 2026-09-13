using Unity.Entities;

public struct RoadParent : IComponentData
{
    public RoadSegmentEnt SegmentEnt;
    public Entity Parent;
}

public struct RoadJsonStringByte : IBufferElementData
{ public byte Value; }