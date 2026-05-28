using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct rgLoadRoadFromJson : IComponentData
{
    public FixedString512Bytes fileName;
}

public struct rgSaveRoadFromJson : IComponentData
{
    public FixedString512Bytes fileName;
}

public struct rgSpawnNodeOrder : IComponentData
{
    public float3 position;
}

public struct rgSpawnEdgeOrder : IComponentData
{
    public Entity Node0;
    public Entity Node1;
}

public struct rgHasMeshTag : IComponentData
{ }

public struct rgRoadLineData : IComponentData
{
    public Entity StartNode;
    public Entity EndNode;
    public Entity Parent;
}

public struct rgRoadNodeData : IComponentData
{
    public Entity Parent;
}

public struct rgRoadNodeLink : IBufferElementData
{
    public Entity LinkedLine;
    public bool IsOutgoing;
    public readonly bool IsIncoming => !IsOutgoing;
}

public struct rgRoadSegmentData : IComponentData
{

}
public struct rgRoadSegmentLine : IBufferElementData
{
    public Entity Line;
}
