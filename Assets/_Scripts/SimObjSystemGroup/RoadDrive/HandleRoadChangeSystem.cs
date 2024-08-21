using Unity.Burst;
using Unity.Entities;

[UpdateAfter(typeof(VehicleDriveSystem))]
[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct HandleRoadChangeSystem : ISystem
{
    private rgEdgeAspect.Lookup m_EdgesLookup;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_EdgesLookup.Update(ref state);
        new HandleRoadChangeJob { EdgesLookup = m_EdgesLookup }.Schedule();
    }

    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        m_EdgesLookup = new rgEdgeAspect.Lookup(ref state);
    }

    [BurstCompile]
    public partial struct HandleRoadChangeJob : IJobEntity
    {
        public rgEdgeAspect.Lookup EdgesLookup;

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
                EdgesLookup[lastStepEdge].RemoveOccupant(vehicle.Entity);

                if (currentEdge != Entity.Null)
                    EdgesLookup[lastStepEdge].AddOccupant(currentEdge);
            }

            vehicle.LastStepOccupiedEdge.ValueRW.Value = currentEdge;
        }
    }
}