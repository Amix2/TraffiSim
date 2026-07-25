using Unity.Entities;

public struct RoadLaneData : IComponentData
{
    public RoadPortEnt StartPortEnt, EndPortEnt;
    public RoadSegmentEnt Parent;
    public RoadLaneEnt LeftNeighbour, RightNeighbour;
    public float LaneWidth;
}

public struct RoadLaneAspect : IAspect
{
    public struct Lookup
    {
        public LookupSlot<RoadLaneData> RoadLaneDataLookup;

        public void Initialize(ref SystemState state)
        {
            RoadLaneDataLookup.Initialize(ref state);
        }

        /// SystemBase / managed system variant.
        public void Initialize(ComponentSystemBase system)
        {
            RoadLaneDataLookup.Initialize(system);
        }

        public void Update(ref SystemState state)
        {
            RoadLaneDataLookup.Update(ref state);
        }

        public void Update(SystemBase system)
        {
            RoadLaneDataLookup.Update(system);
        }

        public RoadLaneAspect this[Entity e] => new RoadLaneAspect
        {
            Entity = e,
            Data = RoadLaneDataLookup.BindRW(e)
        };
    }

    public Entity Entity;
    public RefRW<RoadLaneData> Data;
}