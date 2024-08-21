using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct AccelerateVehiclesSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<SimConfigComponent>())
            return;
        float dt = SystemAPI.GetSingleton<SimConfigComponent>().DeltaTime;
        new AccelerateVehiclesJob { dt = dt }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct AccelerateVehiclesJob : IJobEntity
    {
        public float dt;

        [BurstCompile]
        private void Execute(ref Velocity velocity, in Acceleration acceleration, in MaxVelocity maxVelocity, in VehicleTag _)
        {
            velocity.Value = math.clamp(velocity + acceleration * dt, 0, maxVelocity);
        }
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}