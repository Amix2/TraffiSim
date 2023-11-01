using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class Spawner
{
    public static Entity SpawnVehicle(EntityManager manager, Entity prefab, float3 position, float3 targetDestination)
    {
        Entity vehicle = manager.Instantiate(prefab);
        manager.AddComponent<Velocity>(vehicle);
        manager.AddComponentData(vehicle, new TargetPosition { Value = targetDestination });
        manager.AddComponentData(vehicle, new Acceleration { Value = 1.0f });
        manager.AddComponentData(vehicle, new MaxVelocity { Value = 1.0f });
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