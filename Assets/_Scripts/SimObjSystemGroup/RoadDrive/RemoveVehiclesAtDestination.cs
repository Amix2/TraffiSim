using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct RemoveVehiclesAtDestination : ISystem
{
    private VehicleAspect.Lookup m_VehicleAspectLookup;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_VehicleAspectLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        new RemoveVehiclesAtDestinationJob { ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(), m_VehicleAspectLookup = m_VehicleAspectLookup }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct RemoveVehiclesAtDestinationJob : IJobEntity
    {
        [ReadOnly] public VehicleAspect.Lookup m_VehicleAspectLookup;

        public EntityCommandBuffer.ParallelWriter ECB;
        private const float range = 0.01f;
        private const float rangeSq = range * range;

        [BurstCompile]
        public void Execute(Entity entity, in VehicleTag tag, [EntityIndexInQuery] int sortKey)
        {
            VehicleAspect vehicle = m_VehicleAspectLookup[entity];
            if (vehicle.IsAtDestination(rangeSq))
                ECB.DestroyEntity(sortKey, vehicle.Entity);
        }
    }

    private void OnCreate(ref SystemState state)
    { 
        m_VehicleAspectLookup = new VehicleAspect.Lookup(ref state);
    }

    private void OnDestroy(ref SystemState state)
    { }
}