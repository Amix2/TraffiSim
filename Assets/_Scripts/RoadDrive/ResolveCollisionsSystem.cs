using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static VehicleAspect;

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
        new UpdatePositionTimePoints { timeHorison = 100, timeGap = 1 }.ScheduleParallel();
        new FindFutureCollisionTime { vehicles = Vehicles.AsDeferredJobArray(), VechicleAspects = VechicleAspects, dt = dt }.ScheduleParallel();
        new LimitVelocity { safeTimeHorison = 10 }.ScheduleParallel();
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
    partial struct FindFutureCollisionTime : IJobEntity
    {
        [ReadOnly]
        public NativeArray<VehicleData> vehicles;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public VehicleAspect.Lookup VechicleAspects;
        public float dt;


        private float GetLimitedVelocity(float3 myPos, float3 crashPos, float velocity, float safeTime)
        {
            float distance = (myPos - crashPos).length();
            float maxVel = distance / safeTime;
            return math.min(maxVel, velocity);
        }

        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            float fClosestCollisionDistance = float.MaxValue;
            for (int otherVehID = 0; otherVehID < vehicles.Length; otherVehID++)
            {
                if (vehicles[otherVehID].entity == vehicle.Entity)
                    continue;
                VehicleAspect otherVehicle = VechicleAspects[vehicles[otherVehID].entity];

                for (int myObbID = 0; myObbID < vehicle.GetFutureObbCount(); myObbID++)
                {
                    FutureOBB myFutureObb = vehicle.GetFutureOBBFromId(myObbID);
                    FutureOBB otherFutureObb = otherVehicle.GetFutureOBB(myFutureObb.fTime);
                    bool bCollision = myFutureObb.obb.Intersects(otherFutureObb.obb, 0);
                    float dist = math.distance(myFutureObb.obb.Position, otherFutureObb.obb.Position);
                    if (bCollision)
                    {
                        fClosestCollisionDistance = math.min(fClosestCollisionDistance, myFutureObb.fDistance);
                        continue;
                    }
                }
                
            }
            vehicle.FutureCollisionDistance = fClosestCollisionDistance;
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

    [BurstCompile]
    partial struct LimitVelocity : IJobEntity
    {
        public float safeTimeHorison;
        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            float collistioDistance = vehicle.FutureCollisionDistance;
            float safeDistance = vehicle.LinVelocity * safeTimeHorison;

            if (collistioDistance >= safeDistance)
                return;

            vehicle.LinVelocity = collistioDistance / safeTimeHorison;

            ConsoleLogUI.Log(vehicle.LinVelocity, collistioDistance);
        }
    }

    [BurstCompile]
    partial struct UpdatePositionTimePoints : IJobEntity
    {
        public float timeHorison;
        public float timeGap;
        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            vehicle.UpdatePositionTimePoints(timeHorison, timeGap);
        }

    }
}