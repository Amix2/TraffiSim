using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
        const float range = 0.01f;
        const float rangeSq = range * range;

        [BurstCompile]
        public void Execute(Entity entity, in DestinationPosition targetPosition, in LocalToWorld localToWorld, in VehicleTag _, [EntityIndexInQuery] int sortKey)
        {
            if((targetPosition - localToWorld.Position).lengthsq() < rangeSq) 
                ECB.DestroyEntity(sortKey, entity);
        }
    }

    private void OnCreate(ref SystemState state)
    { }

    private void OnDestroy(ref SystemState state)
    { }
}