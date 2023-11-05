using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct rgNodeEdges : IBufferElementData
{
    public Entity OtherNodeEnt;
    public Entity EdgeEnt;
}

public readonly partial struct rgNodeAspect : IAspect
{
    public readonly Entity Entity;
    //private readonly RefRW<LocalTransform> NodePos;
    private readonly RefRO<LocalTransform> NodePosRO;
    private readonly DynamicBuffer<rgNodeEdges> Neighbours;

    public float3 Position => NodePosRO.ValueRO.Position;
    public NativeArray<rgNodeEdges> NighboursEntities => Neighbours.AsNativeArray();
}
