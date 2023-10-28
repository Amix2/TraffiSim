using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class Spawner
{
    public static Entity SpawnVehicle(EntityManager manager, Entity prefab, float3 position)
    {
        Entity vehicle = manager.Instantiate(prefab);
        manager.AddComponent<Velocity>(vehicle);
        manager.AddBuffer<PathBuffer>(vehicle);
        manager.GetBuffer<PathBuffer>(vehicle).Add(new PathBuffer { Position = new float3(3, 0, 3) });
        manager.GetBuffer<PathBuffer>(vehicle).Add(new PathBuffer { Position = new float3(10, 0, 20) });
        manager.SetComponentData(vehicle, new LocalTransform
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1
        });
        return vehicle;
    }
}