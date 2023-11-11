using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class LoadRoadFromJsonMsg : ISingleMessage
{
    public void Execute(SystemBase systemBase)
    {
        var ecb = systemBase.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        NativeList<ComponentType> types = new(2, Allocator.Temp)
        {
           ComponentType.ReadOnly<rgLoadRoadFromJson>()
        };

        var arch = systemBase.EntityManager.CreateArchetype(types.AsArray());
        ecb.CreateEntity(arch);
    }
}
