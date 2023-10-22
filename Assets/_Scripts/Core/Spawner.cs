using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.UIElements;

public static class Spawner 
{
    public static Entity SpawnVehicle(EntityManager manager, Entity prefab, float3 position)
    {
        Entity vehicle =  manager.Instantiate(prefab);
        manager.SetComponentData(vehicle, new LocalTransform
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1
        });
        return vehicle;
    }

}
