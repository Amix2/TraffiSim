using Unity.Entities;
using Unity.Mathematics;

public struct RoadPortData : IComponentData
{
    public float4x4 Transform;

    public RoadNodeEnt ParentNodeEnt;

    public float3 Right => new float3(Transform.c0.x, Transform.c0.y, Transform.c0.z);
    public float3 Up => new float3(Transform.c1.x, Transform.c1.y, Transform.c1.z);
    public float3 Forward => new float3(Transform.c2.x, Transform.c2.y, Transform.c2.z);
    public float3 Position => new float3(Transform.c3.x, Transform.c3.y, Transform.c3.z);
    public quaternion Rotation => new quaternion(math.orthonormalize(new float3x3(Transform)));
}

public struct RoadPortInput : IBufferElementData
{
    public RoadLaneEnt RoadLaneEnt;
}

public struct RoadPortOutput : IBufferElementData
{
    public RoadLaneEnt RoadLaneEnt;
}

public struct RoadPortRemoveDuplicatesInOutBuffers : IComponentData, IEnableableComponent
{
}

public struct RoadPortAspect : IAspect
{
    public struct Lookup
    {
        public LookupSlot<RoadPortData> RoadPortDataLookup;
        public BufferSlot<RoadPortInput> RoadPortInputLookup;
        public BufferSlot<RoadPortOutput> RoadPortOutputLookup;

        public void Initialize(ref SystemState state)
        {
            RoadPortDataLookup.Initialize(ref state);
            RoadPortInputLookup.Initialize(ref state);
            RoadPortOutputLookup.Initialize(ref state);
        }

        /// SystemBase / managed system variant.
        public void Initialize(ComponentSystemBase system)
        {
            RoadPortDataLookup.Initialize(system);
            RoadPortInputLookup.Initialize(system);
            RoadPortOutputLookup.Initialize(system);
        }

        public void Update(ref SystemState state)
        {
            RoadPortDataLookup.Update(ref state);
            RoadPortInputLookup.Update(ref state);
            RoadPortOutputLookup.Update(ref state);
        }

        public void Update(SystemBase system)
        {
            RoadPortDataLookup.Update(system);
            RoadPortInputLookup.Update(system);
            RoadPortOutputLookup.Update(system);
        }

        public RoadPortAspect this[Entity e] => new RoadPortAspect
        {
            Entity = e,
            Data = RoadPortDataLookup.BindRW(e),
            Inputs = RoadPortInputLookup.Bind(e),
            Outputs = RoadPortOutputLookup.Bind(e)
        };
    }

    public Entity Entity;
    public RefRW<RoadPortData> Data;
    public DynamicBuffer<RoadPortInput> Inputs;
    public DynamicBuffer<RoadPortOutput> Outputs;
}