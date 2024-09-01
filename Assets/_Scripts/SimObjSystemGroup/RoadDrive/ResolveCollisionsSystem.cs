using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using static VehicleAspect;

[UpdateAfter(typeof(AccelerateVehiclesSystem))]
[UpdateBefore(typeof(VehicleDriveSystem))]
[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct ResolveCollisionsSystem : ISystem
{
    private struct VehicleData
    {
        public Entity entity;
    }

    private NativeList<VehicleData> Vehicles;
    VehicleAspect.Lookup VechicleAspects;

   [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.GetSingleton<SimConfigComponent>().DeltaTime;
        Vehicles.Clear();
        VechicleAspects.Update(ref state);
        new GatherDataJob { vehicles = Vehicles.AsParallelWriter() }.ScheduleParallel();
        new UpdatePositionTimePoints { timeHorison = 2, pointsPerSec = 2 }.ScheduleParallel();
        new FindFutureCollisionTime { vehicles = Vehicles.AsDeferredJobArray(), VechicleAspects = VechicleAspects, dt = dt }.ScheduleParallel();
        new LimitVelocity { safeTimeHorison = 1 }.ScheduleParallel();
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimConfigComponent>();
        Vehicles = new NativeList<VehicleData>(1024, Allocator.Persistent);
        VechicleAspects = new VehicleAspect.Lookup(ref state);

    }

    public void OnDestroy(ref SystemState state)
    {
        if (Vehicles.IsCreated) Vehicles.Dispose();
    }

    [BurstCompile]
    private partial struct FindFutureCollisionTime : IJobEntity
    {
        [ReadOnly]
        public NativeArray<VehicleData> vehicles;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public VehicleAspect.Lookup VechicleAspects;

        public float dt;

        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            float fClosestCollisionDistance = float.MaxValue;
            for (int otherVehID = 0; otherVehID < vehicles.Length; otherVehID++)
            {
                Entity otherEnt = vehicles[otherVehID].entity;
                if (otherEnt == vehicle.Entity)
                    continue;

                VehicleAspect otherVehicle = VechicleAspects[otherEnt];

                bool bSkip = false;

                bSkip = vehicle.HasHigherPriorityThan(otherVehicle);

                // other is blocking us, so we have to stop
                if (vehicle.IsBlockedBy(otherVehicle))
                    bSkip = false;

                // we are blocking other, so we have to ignore its route
                if (otherVehicle.IsBlockedBy(vehicle))
                    bSkip = true;

                if (bSkip)
                    continue;

                for (int myObbID = 0; myObbID < vehicle.GetFutureObbCount(); myObbID++)
                {
                    FutureOBB myFutureObb = vehicle.GetFutureOBBFromId(myObbID);
                    FutureOBB otherFutureObb = otherVehicle.GetFutureOBBFromTime(myFutureObb.fTime);
                    if (!otherFutureObb.IsValid())
                        continue;
                    bool bCollision = myFutureObb.obb.Intersects(otherFutureObb.obb, 0.0f);
                    if (bCollision)
                    {
                        fClosestCollisionDistance = math.min(fClosestCollisionDistance, myFutureObb.fDistance);
                        break;
                    }
                }
            }
            vehicle.FutureCollisionDistance = fClosestCollisionDistance - 0.1f;
        }
    }

    [BurstCompile]
    private partial struct GatherDataJob : IJobEntity
    {
        public NativeList<VehicleData>.ParallelWriter vehicles;

        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            vehicles.AddNoResize(new VehicleData { entity = vehicle.Entity });
        }
    }

    [BurstCompile]
    private partial struct LimitVelocity : IJobEntity
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
        }
    }

    [BurstCompile]
    private partial struct UpdatePositionTimePoints : IJobEntity
    {
        public float timeHorison;
        public float pointsPerSec;

        [BurstCompile]
        private void Execute(VehicleAspect vehicle)
        {
            vehicle.UpdatePositionTimePoints(timeHorison, pointsPerSec);
        }
    }
}