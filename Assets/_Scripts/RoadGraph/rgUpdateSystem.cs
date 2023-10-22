using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct rgUpdateSystem : ISystem
{
    private ComponentLookup<LocalToWorld> LocalToWorldPositions;


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        LocalToWorldPositions.Update(ref state);

        new UpdateEdgesPositions { LocalToWorldPositions = LocalToWorldPositions }.ScheduleParallel();
    }


    [BurstCompile]
    public partial struct UpdateEdgesPositions : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalToWorld> LocalToWorldPositions;

        [BurstCompile]
        private void Execute(in rgEdge edge, ref rgEdgePosiotions edgePosiotions)
        {
            edgePosiotions.Pos1 = LocalToWorldPositions[edge.Node1].Position;
            edgePosiotions.Pos2 = LocalToWorldPositions[edge.Node2].Position;
        }
    }



    [BurstCompile]
    void OnCreate(ref SystemState state) 
    {
        LocalToWorldPositions = state.GetComponentLookup<LocalToWorld>(true);

    }

    [BurstCompile]
    void OnDestroy(ref SystemState state) { }
}
