using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct AccelerateVehiclesSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = 0.05f;
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