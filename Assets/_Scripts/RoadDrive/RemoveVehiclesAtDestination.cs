using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct RemoveVehiclesAtDestination : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        new RemoveVehiclesAtDestinationJob { ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter() }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct RemoveVehiclesAtDestinationJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        private const float range = 0.01f;
        private const float rangeSq = range * range;

        [BurstCompile]
        public void Execute(VehicleAspect vehicle, [EntityIndexInQuery] int sortKey)
        {
            if (vehicle.IsAtDestination(rangeSq))
                ECB.DestroyEntity(sortKey, vehicle.Entity);
        }
    }

    private void OnCreate(ref SystemState state)
    { }

    private void OnDestroy(ref SystemState state)
    { }
}