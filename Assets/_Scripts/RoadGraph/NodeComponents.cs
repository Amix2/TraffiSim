using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct rgOutgoingNodeEdges : IBufferElementData
{
    public Entity OtherNodeEnt;
    public Entity EdgeEnt;
}

public struct rgIncomingNodeEdges : IBufferElementData
{
    public Entity OtherNodeEnt;
    public Entity EdgeEnt;
}

public readonly partial struct rgNodeAspect : IAspect
{
    public readonly Entity Entity;

    //private readonly RefRW<LocalTransform> NodePos;
    private readonly RefRO<LocalTransform> NodePosRO;

    private readonly DynamicBuffer<rgOutgoingNodeEdges> OutgoingNeighbours;
    private readonly DynamicBuffer<rgIncomingNodeEdges> IncomingNeighbours;

    public float3 Position => NodePosRO.ValueRO.Position;
    public NativeArray<rgOutgoingNodeEdges> OutgoingNighboursEntities => OutgoingNeighbours.AsNativeArray();
    public NativeArray<rgIncomingNodeEdges> IncomingNighboursEntities => IncomingNeighbours.AsNativeArray();
}