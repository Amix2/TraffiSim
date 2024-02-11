using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AccelerateVehiclesSystem))]
[UpdateBefore(typeof(VehicleDriveSystem))]
public partial struct ResolveCollisionsSystem : ISystem
{
    struct VehicleData
    {
        public Entity entity;
    }
    NativeList<VehicleData> Vehicles;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.GetSingleton<SimConfigComponent>().DeltaTime;
        Vehicles.Clear();
        VehicleAspect.Lookup VechicleAspects = new VehicleAspect.Lookup(ref state);
        VechicleAspects.Update(ref state);
        new GatherDataJob { vehicles = Vehicles.AsParallelWriter() }.ScheduleParallel();
        new LimitVelocityJob { vehicles = Vehicles.AsDeferredJobArray(), VechicleAspects = VechicleAspects }.Schedule();
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimConfigComponent>();
        Vehicles = new NativeList<VehicleData>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (Vehicles.IsCreated) Vehicles.Dispose();
    }

    [BurstCompile]
    partial struct LimitVelocityJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<VehicleData> vehicles;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public VehicleAspect.Lookup VechicleAspects;

        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            float timeHorizont = 10;
            float minDistance = 2;
            for (int i = 0; i < vehicles.Length; i++)
            {
                if(vehicles[i].entity == vehicle.Entity) 
                    continue;
                float range = vehicle.LinVelocity * timeHorizont + minDistance;
                var intercection = vehicle.GetPathIntersection(VechicleAspects[vehicles[i].entity], minDistance, range);
                if (!intercection.IsNull)
                {
                    float newMaxVel = math.max(0, intercection.MyDistance - minDistance) / timeHorizont; 
                    vehicle.LinVelocity = math.min(vehicle.LinVelocity, newMaxVel);
                    //vehicle.LinVelocity = 0;
                }
            }
        }
    }
    [BurstCompile]
    partial struct GatherDataJob : IJobEntity
    {
        public NativeList<VehicleData>.ParallelWriter vehicles;
        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            vehicles.AddNoResize(new VehicleData { entity = vehicle.Entity });
        }
    }
}