using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;


public partial struct VehicleDriveSystem : ISystem
{
    rgEdgeAspect.Lookup m_EdgesLookup;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;
        m_EdgesLookup.Update(ref state);

        rgRoadManagerAspect RoadManager = SystemAPI.GetAspect<rgRoadManagerAspect>(SystemAPI.GetSingletonEntity<rgRoadManager>());

        float dt = 0.05f;
        new AccelerateVehiclesJob { dt = dt }.ScheduleParallel();
        new NavMeshJob { RoadManager = RoadManager, EdgesLookup = m_EdgesLookup }.ScheduleParallel();

        new SavePositionJob { }.ScheduleParallel();
        new MoveVehicleJob { dt = dt }.ScheduleParallel();
        new SetVehicleOrientation { }.ScheduleParallel();

    }

    [BurstCompile]
    public partial struct NavMeshJob : IJobEntity
    {
        [ReadOnly] public rgRoadManagerAspect RoadManager;
        [ReadOnly] public rgEdgeAspect.Lookup EdgesLookup;

        [BurstCompile]
        private void Execute(ref DynamicBuffer<PathBuffer> path, in LocalTransform transform, in DestinationPosition target)
        {
            if (!path.IsEmpty)
                return;

            var closestRoad = RoadManager.GetClosestRoad(transform.Position, EdgesLookup);
            var closestEndRoad = RoadManager.GetClosestRoad(target, EdgesLookup);

            float distToTargetSq = (transform.Position - target).lengthsq();
            float distToClosestSq = (transform.Position - closestRoad.RoadPosition).lengthsq();
            float distToEndSq = (target - closestEndRoad.RoadPosition).lengthsq();

            bool directPath = distToTargetSq < distToClosestSq + distToEndSq;

            if(!directPath)
            {
                path.Add(new PathBuffer { Position = closestRoad.RoadPosition, Target = closestRoad.Edge });
                // some smart path finding
                path.Add(new PathBuffer { Position = closestEndRoad.RoadPosition, Target = closestRoad.Edge });
            }
            path.Add(new PathBuffer { Position = target, Target = Entity.Null });


        }
    }

    [BurstCompile]
    public partial struct MoveVehicleJob : IJobEntity
    {
        public float dt;
        [BurstCompile]
        private void Execute(ref LocalTransform transform, ref DynamicBuffer<PathBuffer> path, in Velocity velocity, in VehicleTag _)
        {
            float3 position = transform.Position;
            float distLeft = velocity * dt;
            while (distLeft > 0 && !path.IsEmpty)
            {
                float3 dir = path[0].Position - position;
                float dirLen = dir.length();
                if(dirLen == 0)
                {
                    path.RemoveAt(0);
                    continue;
                }
                if (dirLen < distLeft)
                {   // jump to next vertes
                    distLeft -= dirLen;
                    position = path[0].Position;
                    path.RemoveAt(0);
                }
                else
                {   // move along the edge
                    position += dir.norm() * distLeft;
                    distLeft = 0;
                }
            }
            transform.Position = position;
        }
    }

    [BurstCompile]
    public partial struct AccelerateVehiclesJob : IJobEntity
    {
        public float dt;
        [BurstCompile]
        private void Execute(ref Velocity velocity, in Acceleration acceleration, in MaxVelocity maxVelocity, in VehicleTag _)
        {
            velocity.Value = math.clamp(velocity + acceleration * dt, 0, maxVelocity);
        }
    }

    [BurstCompile]
    public partial struct SavePositionJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(ref LastStepPosition lastPos, in LocalTransform localTransform, in VehicleTag _)
        {
            lastPos.Value = localTransform.Position;
        }
    }

    [BurstCompile]
    public partial struct SetVehicleOrientation : IJobEntity
    {
        [BurstCompile]
        private void Execute(ref LocalTransform localTransform, in LastStepPosition lastPos, in VehicleTag _)
        {
            float3 dir = lastPos - localTransform.Position;
            if(dir.lengthsq() > 0)
                localTransform.Rotation = quaternion.LookRotation(dir.norm(), new float3(0,1,0));
        }
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        m_EdgesLookup = new rgEdgeAspect.Lookup(ref state);
    }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    { }
}