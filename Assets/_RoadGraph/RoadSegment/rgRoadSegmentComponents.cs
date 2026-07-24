using Unity.Entities;

public struct rgRoadSegmentData : IComponentData
{
}

[InternalBufferCapacity(2)]
public struct rgRoadSegmentNode : IBufferElementData
{
    public RoadNodeEnt NodeEnt;
}

[InternalBufferCapacity(2)]
public struct rgRoadSegmentLane : IBufferElementData
{
    public RoadLaneEnt NodeEnt;
}

public struct RoadSegmentAspect : IAspect
{
    public struct Lookup
    {
        public LookupSlot<rgRoadSegmentData> rgRoadSegmentDataLookup;
        public BufferSlot<rgRoadSegmentNode> rgRoadSegmentNodeLookup;
        public BufferSlot<rgRoadSegmentLane> rgRoadSegmentLanesLookup;
        public void Initialize(ref SystemState state)
        {
            rgRoadSegmentDataLookup.Initialize(ref state);
            rgRoadSegmentNodeLookup.Initialize(ref state);
            rgRoadSegmentLanesLookup.Initialize(ref state);
        }
        /// SystemBase / managed system variant.
        public void Initialize(ComponentSystemBase system)
        {
            rgRoadSegmentDataLookup.Initialize(system);
            rgRoadSegmentNodeLookup.Initialize(system);
            rgRoadSegmentLanesLookup.Initialize(system);
        }
        public void Update(ref SystemState state)
        {
            rgRoadSegmentDataLookup.Update(ref state);
            rgRoadSegmentNodeLookup.Update(ref state);
            rgRoadSegmentLanesLookup.Update(ref state);
        }
        public void Update(SystemBase system)
        {
            rgRoadSegmentDataLookup.Update(system);
            rgRoadSegmentNodeLookup.Update(system);
            rgRoadSegmentLanesLookup.Update(system);
        }
        public RoadSegmentAspect this[Entity e] => new RoadSegmentAspect
        {
            Entity = e,
            Data = rgRoadSegmentDataLookup.BindRW(e),
            ChildNodes = rgRoadSegmentNodeLookup.Bind(e),
            ChildLanes = rgRoadSegmentLanesLookup.Bind(e)
        };
    }

    public Entity Entity;
    public RefRW<rgRoadSegmentData> Data;
    public DynamicBuffer<rgRoadSegmentNode> ChildNodes;
    public DynamicBuffer<rgRoadSegmentLane> ChildLanes;
}
