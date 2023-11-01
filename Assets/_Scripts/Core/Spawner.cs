using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class Spawner
{
    public static Entity SpawnVehicle(EntityManager manager, Entity prefab, float3 position, float3 targetDestination)
    {
        Entity vehicle = manager.Instantiate(prefab);
        manager.AddComponent<VehicleTag>(vehicle);
        manager.AddComponent<Velocity>(vehicle);
        manager.AddComponentData(vehicle, new DestinationPosition { Value = targetDestination });
        manager.AddComponentData(vehicle, new Acceleration { Value = 1.0f });
        manager.AddComponentData(vehicle, new MaxVelocity { Value = 1.0f });
        manager.AddComponentData(vehicle, new LastStepPosition { Value = position });
        manager.AddBuffer<PathBuffer>(vehicle);
        manager.SetComponentData(vehicle, new LocalTransform
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1
        });
        return vehicle;
    }
}