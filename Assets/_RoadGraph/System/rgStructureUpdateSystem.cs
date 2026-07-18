using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[WithAll(typeof(RoadLaneNodeUpdateInOutBuffers))]
public partial struct UpdateNodesInOutBuffers : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(Entity entity,
            [EntityIndexInQuery] int sortKey,
            ref DynamicBuffer<RoadLaneNodeInput> inputs, ref DynamicBuffer<RoadLaneNodeOutput> outputs)
    {
        // remove duplicates
        for (int i = 0; i < inputs.Length; i++)
        {
            bool duplicate = false;
            for (int j = 0; j < i && duplicate == false; j++)
                if (inputs[i].RoadLaneEnt == inputs[j].RoadLaneEnt)
                    duplicate = true;
            if (duplicate)
            {
                inputs.RemoveAtSwapBack(i);
                i--;
            }
        }
        for (int i = 0; i < outputs.Length; i++)
        {
            bool duplicate = false;
            for (int j = 0; j < i && duplicate == false; j++)
                if (outputs[i].RoadLaneEnt == outputs[j].RoadLaneEnt)
                    duplicate = true;
            if (duplicate)
            {
                outputs.RemoveAtSwapBack(i);
                i--;
            }
        }
        ECB.SetComponentEnabled<RoadLaneNodeUpdateInOutBuffers>(sortKey, entity, false);
    }
}

public partial struct rgStructureUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RoadLaneNodeUpdateInOutBuffers>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.Dependency = new UpdateNodesInOutBuffers { ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter() }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}