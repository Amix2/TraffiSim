using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


public struct RoadParent : IComponentData
{
    public RoadSegmentEnt SegmentEnt;
    public Entity Parent;
}


public struct RoadJsonStringByte : IBufferElementData { public byte Value; }
