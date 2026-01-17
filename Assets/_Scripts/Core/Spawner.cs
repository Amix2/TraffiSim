using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class Spawner
{
    public static Entity SpawnVehicle(EntityCommandBuffer ecb, Entity prefab, float3 position, float3 targetDestination)
    {
        Entity vehicle = ecb.Instantiate(prefab);
        ecb.AddComponent<VehicleTag>(vehicle);
        ecb.AddComponent<Velocity>(vehicle);
        ecb.AddComponent(vehicle, new DestinationPosition { Value = targetDestination });
        ecb.AddComponent(vehicle, new Acceleration { Value = 10.0f });
        ecb.AddComponent(vehicle, new MaxVelocity { Value = 20.0f });
        ecb.AddComponent(vehicle, new LastStepPosition { Value = position });
        ecb.AddComponent(vehicle, new FutureCollisionDistanceC { Value = float.MaxValue });
        ecb.AddComponent(vehicle, new Priority { Value = 1 });
        ecb.AddBuffer<PathBuffer>(vehicle);
        ecb.AddBuffer<PositionTimePoint>(vehicle);
        ecb.SetComponent(vehicle, new LocalTransform
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1
        });
        return vehicle;
    }

    public static Entity SpawnFactory(EntityManager manager, Entity prefab, float3 position, quaternion orientation)
    {
        Entity ent = manager.Instantiate(prefab);

        return ent;
    }
}