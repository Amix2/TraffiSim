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

public struct rgNodeAspectOLD
{
    public Entity Entity;

    public RefRO<LocalTransform> NodePosRO;

    public DynamicBuffer<rgOutgoingNodeEdges> OutgoingNeighbours;
    public DynamicBuffer<rgIncomingNodeEdges> IncomingNeighbours;

    public float3 Position => NodePosRO.ValueRO.Position;
    public NativeArray<rgOutgoingNodeEdges> OutgoingNighboursEntities => OutgoingNeighbours.AsNativeArray();
    public NativeArray<rgIncomingNodeEdges> IncomingNighboursEntities => IncomingNeighbours.AsNativeArray();

    public static rgNodeAspectOLD Make(Entity entity,
        ComponentLookup<LocalTransform> localTransformLookup,
        BufferLookup<rgOutgoingNodeEdges> outgoingBufferLookup,
        BufferLookup<rgIncomingNodeEdges> incomingBufferLookup)
    {
        rgNodeAspectOLD aspect = new()
        {
            Entity = entity,
            NodePosRO = localTransformLookup.GetRefRO(entity),
            OutgoingNeighbours = outgoingBufferLookup[entity],
            IncomingNeighbours = incomingBufferLookup[entity],
        };
        return aspect;
    }
    public static rgNodeAspectOLD Make(Entity entity, ComponentLookup<LocalTransform> localTransformLookup)
    {
        rgNodeAspectOLD aspect = new()
        {
            Entity = entity,
            NodePosRO = localTransformLookup.GetRefRO(entity),
        };
        return aspect;
    }
    public static rgNodeAspectOLD Make(Entity entity, BufferLookup<rgOutgoingNodeEdges> outgoingBufferLookup)
    {
        rgNodeAspectOLD aspect = new()
        {
            Entity = entity,
            OutgoingNeighbours = outgoingBufferLookup[entity],
        };
        return aspect;
    }
    public static rgNodeAspectOLD Make(Entity entity, BufferLookup<rgIncomingNodeEdges> incomingBufferLookup)
    {
        rgNodeAspectOLD aspect = new()
        {
            Entity = entity,
            IncomingNeighbours = incomingBufferLookup[entity],
        };
        return aspect;
    }
}