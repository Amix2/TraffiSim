using Unity.Burst;
using Unity.Entities;

[UpdateAfter(typeof(VehicleDriveSystem))]
[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct HandleRoadChangeSystem : ISystem
{
    private BufferLookup<rgEdgeOccupant> m_EdgesLookup;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_EdgesLookup.Update(ref state);
        new HandleRoadChangeJob { EdgesLookup = m_EdgesLookup }.Schedule();
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        m_EdgesLookup = SystemAPI.GetBufferLookup<rgEdgeOccupant>();
    }

    [BurstCompile]
    public partial struct HandleRoadChangeJob : IJobEntity
    {
        public BufferLookup<rgEdgeOccupant> EdgesLookup;

        [BurstCompile]
        private void Execute(VehicleAspect vehicle, [EntityIndexInQuery] int sortKey)
        {
            if (vehicle.PathBuffer.IsEmpty)
                return;
            Entity currentEdge = vehicle.PathBuffer[0].EdgeEnt;
            Entity lastStepEdge = vehicle.LastStepOccupiedEdge.ValueRO.Value;
            if (lastStepEdge == currentEdge)
            {
                vehicle.LastStepOccupiedEdge.ValueRW.Value = currentEdge;
                return;
            }

            if (lastStepEdge != Entity.Null)
            {
                rgEdgeAspect edge = rgEdgeAspect.Make(lastStepEdge, EdgesLookup);

                if (currentEdge != Entity.Null)
                    edge.AddOccupant(currentEdge);
            }

            vehicle.LastStepOccupiedEdge.ValueRW.Value = currentEdge;
        }
    }
}