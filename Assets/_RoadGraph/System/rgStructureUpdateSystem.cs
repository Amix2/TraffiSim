using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct UpdatePortsInOutBuffers : IJobEntity
{
    public void Execute(Entity entity,
            ref DynamicBuffer<RoadPortInput> inputs, ref DynamicBuffer<RoadPortOutput> outputs, EnabledRefRW<RoadPortRemoveDuplicatesInOutBuffers> update)
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
        update.ValueRW = false;
    }
}

[BurstCompile]
public partial struct UpdateRoadLanePoints : IJobEntity
{
    [ReadOnly] public RoadPortAspect.Lookup PortLookup;

    public void Execute(Entity entity,
            RefRW<RoadLaneData> roadLaneData, DynamicBuffer<RoadLanePoint> roadLanePoints, EnabledRefRW<RoadLaneUpdatePoints> update)
    {
        roadLanePoints.Clear();
        RoadPortAspect startPort = PortLookup[roadLaneData.ValueRO.StartPortEnt];
        RoadPortAspect endPort = PortLookup[roadLaneData.ValueRO.EndPortEnt];
        roadLanePoints.Add(new RoadLanePoint { Position = startPort.Position, Distance = 0f });
        roadLanePoints.Add(new RoadLanePoint { Position = endPort.Position, Distance = 0f });
        float3 lastPos = startPort.Position;
        for (int i = 0; i < roadLanePoints.Length; i++)
        {
            float3 pos = roadLanePoints[i].Position;
            float dist = math.distance(lastPos, pos);
            roadLanePoints.ElementAt(i).Distance = dist;
            lastPos = pos;
        }
        update.ValueRW = false;
    }
}

public partial struct rgStructureUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RoadPortAspectLookup.Initialize(ref state);
        RoadPortAspectLookup.LocalTransformLookup.Request(ref state, true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RoadPortAspectLookup.Update(ref state);
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.Dependency = new UpdatePortsInOutBuffers { }.ScheduleParallel(state.Dependency);
        state.Dependency = new UpdateRoadLanePoints { PortLookup = RoadPortAspectLookup }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    public RoadPortAspect.Lookup RoadPortAspectLookup;
}