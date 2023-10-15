using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UIElements;

public static class Spawner 
{
    public static Entity SpawnVehicle(EntityManager manager, Entity prefab, float3 position)
    {
        Entity vehicle =  manager.Instantiate(prefab);
        return vehicle;
    }

}
