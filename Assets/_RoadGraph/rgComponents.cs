using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


public struct RoadParent : IComponentData
{
    public RoadSegmentEnt SegmentEnt;
    public Entity Parent;
}

public struct RoadVisualizerParent : IComponentData
{
    public Entity ParentEnt;
}

public struct RoadJsonStringByte : IBufferElementData { public byte Value; }
