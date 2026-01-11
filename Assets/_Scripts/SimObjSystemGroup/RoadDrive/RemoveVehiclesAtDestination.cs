using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct RemoveObjectsAtDestination : ISystem
{
    private ComponentLookup<DestinationPosition> m_DestinationPositionLookup;
    private ComponentLookup<LocalTransform> m_LocalTransformLookup;
    

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_DestinationPositionLookup.Update(ref state);
        m_LocalTransformLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        new RemoveVehiclesAtDestinationJob { ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(), m_DestinationPositionLookup = m_DestinationPositionLookup, m_LocalTransformLookup = m_LocalTransformLookup }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct RemoveVehiclesAtDestinationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<DestinationPosition> m_DestinationPositionLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> m_LocalTransformLookup;

        public EntityCommandBuffer.ParallelWriter ECB;
        private const float range = 0.01f;
        private const float rangeSq = range * range;

        public readonly bool IsAtDestination(float3 Destination, float3 Position)
        { 
            return (Destination - Position).lengthsq() < rangeSq; 
        }

        [BurstCompile]
        public void Execute(Entity entity, in VehicleTag _, [EntityIndexInQuery] int sortKey)
        {
            float3 destination = m_DestinationPositionLookup[entity];
            float3 Position = m_LocalTransformLookup[entity].Position;

            if (IsAtDestination(destination, Position))
                ECB.DestroyEntity(sortKey, entity);
        }
    }

    private void OnCreate(ref SystemState state)
    {
        m_DestinationPositionLookup = SystemAPI.GetComponentLookup<DestinationPosition>(true);
        m_LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
    }

    private void OnDestroy(ref SystemState state)
    { }
}