//using Unity.Burst;
//using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
//using Unity.Entities;
//using Unity.Mathematics;

//[UpdateAfter(typeof(AccelerateVehiclesSystem))]
//[UpdateBefore(typeof(VehicleDriveSystem))]
//[UpdateInGroup(typeof(SimObjSystemGroup))]
//public partial struct ResolveCollisionsSystem : ISystem
//{
//    private struct VehicleData
//    {
//        public Entity entity;
//    }

//    private NativeList<VehicleData> Vehicles;
//    private VehicleAspect.Lookup m_VehicleAspectLookup;

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        float dt = SystemAPI.GetSingleton<SimConfigComponent>().DeltaTime;
//        Vehicles.Clear();
//        m_VehicleAspectLookup.Update(ref state);
//        new GatherDataJob { vehicles = Vehicles.AsParallelWriter() }.ScheduleParallel();
//        new UpdatePositionTimePoints { timeHorison = 2, pointsPerSec = 2, m_VehicleAspectLookup = m_VehicleAspectLookup }.ScheduleParallel();
//        new FindFutureCollisionTime { vehicles = Vehicles.AsDeferredJobArray(), m_VehicleAspectLookup = m_VehicleAspectLookup, dt = dt }.ScheduleParallel();
//        new LimitVelocity { safeTimeHorison = 1, m_VehicleAspectLookup = m_VehicleAspectLookup }.ScheduleParallel();
//    }

//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<SimConfigComponent>();
//        Vehicles = new NativeList<VehicleData>(1024, Allocator.Persistent);
//        m_VehicleAspectLookup = new VehicleAspect.Lookup(ref state);
//    }

//    public void OnDestroy(ref SystemState state)
//    {
//        if (Vehicles.IsCreated) Vehicles.Dispose();
//    }

//    [BurstCompile]
//    private partial struct FindFutureCollisionTime : IJobEntity
//    {
//        [ReadOnly]
//        public NativeArray<VehicleData> vehicles;

//        [NativeDisableContainerSafetyRestriction]
//        public VehicleAspect.Lookup m_VehicleAspectLookup;

//        public float dt;

//        [BurstCompile]
//        public void Execute(Entity entity, in VehicleTag tag)
//        {
//            VehicleAspect vehicle = m_VehicleAspectLookup[entity];
//            float fClosestCollisionDistance = float.MaxValue;
//            for (int otherVehID = 0; otherVehID < vehicles.Length; otherVehID++)
//            {
//                Entity otherEnt = vehicles[otherVehID].entity;
//                if (otherEnt == vehicle.Entity)
//                    continue;

//                VehicleAspect otherVehicle = m_VehicleAspectLookup[otherEnt];

//                bool bSkip = false;

//                bSkip = vehicle.HasHigherPriorityThan(otherVehicle);

//                // other is blocking us, so we have to stop
//                if (vehicle.IsBlockedBy(otherVehicle))
//                    bSkip = false;

//                // we are blocking other, so we have to ignore its route
//                if (otherVehicle.IsBlockedBy(vehicle))
//                    bSkip = true;

//                if (bSkip)
//                    continue;

//                for (int myObbID = 0; myObbID < vehicle.GetFutureObbCount(); myObbID++)
//                {
//                    VehicleAspect.FutureOBB myFutureObb = vehicle.GetFutureOBBFromId(myObbID);
//                    VehicleAspect.FutureOBB otherFutureObb = otherVehicle.GetFutureOBBFromTime(myFutureObb.fTime);
//                    if (!otherFutureObb.IsValid())
//                        continue;
//                    bool bCollision = myFutureObb.obb.Intersects(otherFutureObb.obb, 0.05f);
//                    if (bCollision)
//                    {
//                        fClosestCollisionDistance = math.min(fClosestCollisionDistance, myFutureObb.fDistance);
//                        break;
//                    }
//                }
//            }
//            vehicle.FutureCollisionDistance = fClosestCollisionDistance - 0.1f;
//        }
//    }

//    [BurstCompile]
//    private partial struct GatherDataJob : IJobEntity
//    {
//        public NativeList<VehicleData>.ParallelWriter vehicles;

//        [BurstCompile]
//        public void Execute(Entity entity, in VehicleTag tag)
//        {
//            vehicles.AddNoResize(new VehicleData { entity = entity });
//        }
//    }

//    [BurstCompile]
//    private partial struct LimitVelocity : IJobEntity
//    {
//        public float safeTimeHorison;
//        public VehicleAspect.Lookup m_VehicleAspectLookup;

//        [BurstCompile]
//        public void Execute(Entity entity, in VehicleTag tag)
//        {
//            VehicleAspect vehicle = m_VehicleAspectLookup[entity];
//            float collistioDistance = vehicle.FutureCollisionDistance;
//            float safeDistance = vehicle.LinVelocity * safeTimeHorison;

//            if (collistioDistance >= safeDistance)
//                return;

//            vehicle.LinVelocity = collistioDistance / safeTimeHorison;
//        }
//    }

//    [BurstCompile]
//    private partial struct UpdatePositionTimePoints : IJobEntity
//    {
//        public float timeHorison;
//        public float pointsPerSec;
//        public VehicleAspect.Lookup m_VehicleAspectLookup;

//        [BurstCompile]
//        public void Execute(Entity entity, in VehicleTag tag)
//        {
//            VehicleAspect vehicle = m_VehicleAspectLookup[entity];
//            vehicle.UpdatePositionTimePoints(timeHorison, pointsPerSec);
//        }
//    }
//}