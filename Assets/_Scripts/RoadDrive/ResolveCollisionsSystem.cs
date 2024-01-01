using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
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
        Vehicles.Clear();
        new GatherDataJob { vehicles = Vehicles.AsParallelWriter() }.ScheduleParallel();
        new LimitVelocityJob { vehicles = Vehicles.AsParallelReader() }.ScheduleParallel();

    }

    public void OnCreate(ref SystemState state)
    {
        Vehicles = new NativeList<VehicleData>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (Vehicles.IsCreated) Vehicles.Dispose();
    }

    [BurstCompile]
    partial struct LimitVelocityJob : IJobEntity
    {
        public NativeArray<VehicleData>.ReadOnly vehicles;

        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
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