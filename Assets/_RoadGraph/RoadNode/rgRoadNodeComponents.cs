using Unity.Entities;

public struct rgRoadNodeData : IComponentData
{
    public Line2D line2D;
}

[InternalBufferCapacity(2)]
public struct rgRoadPortChild : IBufferElementData
{
    public RoadPortEnt NodeEnt;
}

public struct RoadNodeAspect : IAspect
{
    public struct Lookup
    {
        public LookupSlot<rgRoadNodeData> rgRoadNodeDataLookup;
        public BufferSlot<rgRoadPortChild> rgRoadPortChildLookup;
        public void Initialize(ref SystemState state)
        {
            rgRoadNodeDataLookup.Initialize(ref state);
            rgRoadPortChildLookup.Initialize(ref state);
        }
        /// SystemBase / managed system variant.
        public void Initialize(ComponentSystemBase system)
        {
            rgRoadNodeDataLookup.Initialize(system);
            rgRoadPortChildLookup.Initialize(system);
        }
        public void Update(ref SystemState state)
        {
            rgRoadNodeDataLookup.Update(ref state);
            rgRoadPortChildLookup.Update(ref state);
        }
        public void Update(SystemBase system)
        {
            rgRoadNodeDataLookup.Update(system);
            rgRoadPortChildLookup.Update(system);
        }
        public RoadNodeAspect this[Entity e] => new RoadNodeAspect
        {
            Entity = e,
            Data = rgRoadNodeDataLookup.BindRW(e),
            ChildNodes = rgRoadPortChildLookup.Bind(e)
        };
    }
    public Entity Entity;
    public RefRW<rgRoadNodeData> Data;
    public DynamicBuffer<rgRoadPortChild> ChildNodes;
}
