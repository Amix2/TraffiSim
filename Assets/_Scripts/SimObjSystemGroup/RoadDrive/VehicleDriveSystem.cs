using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct VehicleDriveSystem : ISystem
{
    private rgEdgeAspect.Lookup m_EdgesLookup;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<SimConfigComponent>())
            return;

        m_EdgesLookup.Update(ref state);

        float dt = SystemAPI.GetSingleton<SimConfigComponent>().DeltaTime;

        new SavePositionJob { }.ScheduleParallel();
        new MoveVehicleJob { dt = dt }.ScheduleParallel();
        new SetVehicleOrientation { }.ScheduleParallel();
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
                if (dirLen == 0)
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
            float3 dir = localTransform.Position - lastPos;
            if (dir.lengthsq() > 0)
                localTransform.Rotation = quaternion.LookRotation(dir.norm(), new float3(0, 1, 0));
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