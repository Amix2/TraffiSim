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

public struct rgNodeAspect
{
    public Entity Entity;

    public RefRO<LocalTransform> NodePosRO;

    public DynamicBuffer<rgOutgoingNodeEdges> OutgoingNeighbours;
    public DynamicBuffer<rgIncomingNodeEdges> IncomingNeighbours;

    public float3 Position => NodePosRO.ValueRO.Position;
    public NativeArray<rgOutgoingNodeEdges> OutgoingNighboursEntities => OutgoingNeighbours.AsNativeArray();
    public NativeArray<rgIncomingNodeEdges> IncomingNighboursEntities => IncomingNeighbours.AsNativeArray();

    public static rgNodeAspect Make(Entity entity,
        ComponentLookup<LocalTransform> localTransformLookup,
        BufferLookup<rgOutgoingNodeEdges> outgoingBufferLookup,
        BufferLookup<rgIncomingNodeEdges> incomingBufferLookup)
    {
        rgNodeAspect aspect = new()
        {
            Entity = entity,
            NodePosRO = localTransformLookup.GetRefRO(entity),
            OutgoingNeighbours = outgoingBufferLookup[entity],
            IncomingNeighbours = incomingBufferLookup[entity],
        };
        return aspect;
    }
    public static rgNodeAspect Make(Entity entity, ComponentLookup<LocalTransform> localTransformLookup)
    {
        rgNodeAspect aspect = new()
        {
            Entity = entity,
            NodePosRO = localTransformLookup.GetRefRO(entity),
        };
        return aspect;
    }
    public static rgNodeAspect Make(Entity entity, BufferLookup<rgOutgoingNodeEdges> outgoingBufferLookup)
    {
        rgNodeAspect aspect = new()
        {
            Entity = entity,
            OutgoingNeighbours = outgoingBufferLookup[entity],
        };
        return aspect;
    }
    public static rgNodeAspect Make(Entity entity, BufferLookup<rgIncomingNodeEdges> incomingBufferLookup)
    {
        rgNodeAspect aspect = new()
        {
            Entity = entity,
            IncomingNeighbours = incomingBufferLookup[entity],
        };
        return aspect;
    }
}