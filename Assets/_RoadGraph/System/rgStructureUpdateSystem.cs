using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Assertions;

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
    [ReadOnly] public ComponentLookup<RoadPortData> RoadPortDataLookup;

    public void Execute(Entity entity,
            RefRO<RoadLaneData> roadLaneData, DynamicBuffer<RoadLanePoint> roadLanePoints, EnabledRefRW<RoadLaneUpdatePoints> update)
    {
        roadLanePoints.Clear();
        RoadPortAspect startPort = new RoadPortAspect(roadLaneData.ValueRO.StartPortEnt).Set(RoadPortDataLookup);
        RoadPortAspect endPort = new RoadPortAspect(roadLaneData.ValueRO.EndPortEnt).Set(RoadPortDataLookup);
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

[BurstCompile]
public partial struct UpdateRoadLaneNeighbours : IJobEntity
{
    [ReadOnly] public ComponentLookup<RoadLaneData> RoadLaneDataLookup;
    [ReadOnly] public BufferLookup<RoadNodePortChild> RoadNodePortChildLookup;
    [ReadOnly] public ComponentLookup<RoadPortData> RoadPortDataLookup;
    [ReadOnly] public BufferLookup<RoadSegmentLane> RoadSegmentLaneLookup;

    private RoadLaneAspect FindLaneWithPort(DynamicBuffer<RoadLaneEnt> lanes, in RoadPortEnt port)
    {
        foreach (RoadLaneEnt lane in lanes)
        {
            RoadLaneAspect laneAspect = new RoadLaneAspect(lane).Set(RoadLaneDataLookup);
            if (laneAspect.Data.ValueRO.EndPortEnt == port || laneAspect.Data.ValueRO.StartPortEnt == port)
                return laneAspect;
        }
        return new();
    }

    public void Execute(Entity entity,
            RefRO<RoadLaneData> data, DynamicBuffer<RoadLaneNeighbour> neighbours, EnabledRefRW<RoadLaneUpdateNeighbours> update)
    {
        RoadSegmentAspect parentSegment = new RoadSegmentAspect(data.ValueRO.Parent).Set(RoadSegmentLaneLookup);
        DynamicBuffer<RoadLaneEnt> siblings = parentSegment.ChildLanes.Reinterpret<RoadLaneEnt>();
        RoadPortAspect endPort = new RoadPortAspect(data.ValueRO.EndPortEnt).Set(RoadPortDataLookup);
        RoadNodeAspect endNode = new RoadNodeAspect(endPort.Parent).Set(RoadNodePortChildLookup);
        DynamicBuffer<RoadPortEnt> portsAtEndNode = endNode.ChildPorts.Reinterpret<RoadPortEnt>();

        int MyIndexInNode = -1;
        for (int i = 0; i < portsAtEndNode.Length; i++)
        {
            if (endPort.Entity == portsAtEndNode[i])
            {
                MyIndexInNode = i;
                break;
            }
        }
        Assert.AreNotEqual(-1, MyIndexInNode);
        if (MyIndexInNode == -1)
            return;

        neighbours.Clear();
        int NeiPortID = MyIndexInNode - 1;
        if (NeiPortID >= 0 && NeiPortID < portsAtEndNode.Length)
        {
            RoadLaneAspect neiEnt = FindLaneWithPort(siblings, portsAtEndNode[NeiPortID]);
            if (neiEnt.Entity != Entity.Null)
            {
                bool TheSameDir = neiEnt.Data.ValueRO.StartPortEnt == data.ValueRO.StartPortEnt;
                neighbours.Add(new RoadLaneNeighbour { Entity = neiEnt.Entity, TheSameDirection = TheSameDir });
            }
        }
        NeiPortID = MyIndexInNode + 1;
        if (NeiPortID >= 0 && NeiPortID < portsAtEndNode.Length)
        {
            RoadLaneAspect neiEnt = default;
            bool TheSameDir = false;
            foreach (RoadLaneEnt lane in siblings)
            {
                RoadLaneAspect laneAspect = new RoadLaneAspect(lane).Set(RoadLaneDataLookup);
                if (laneAspect.Data.ValueRO.EndPortEnt == portsAtEndNode[NeiPortID])
                {
                    neiEnt = laneAspect;
                    TheSameDir = true;
                    break;
                }
                if (laneAspect.Data.ValueRO.StartPortEnt == portsAtEndNode[NeiPortID])
                {
                    neiEnt = laneAspect;
                    TheSameDir = false;
                    break;
                }
            }
            if (neiEnt.Entity != Entity.Null)
            {
                neighbours.Add(new RoadLaneNeighbour { Entity = neiEnt.Entity, TheSameDirection = TheSameDir });
            }
        }

        update.ValueRW = false;
    }
}

public partial struct rgStructureUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RoadLaneDataLookup = state.GetComponentLookup<RoadLaneData>(true);
        RoadPortDataLookup = state.GetComponentLookup<RoadPortData>(true);
        RoadNodePortChildLookup = state.GetBufferLookup<RoadNodePortChild>(true);
        RoadSegmentLaneLookup = state.GetBufferLookup<RoadSegmentLane>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RoadLaneDataLookup.Update(ref state);
        RoadPortDataLookup.Update(ref state);
        RoadNodePortChildLookup.Update(ref state);
        RoadSegmentLaneLookup.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.Dependency = new UpdatePortsInOutBuffers { }.ScheduleParallel(state.Dependency);
        state.Dependency = new UpdateRoadLanePoints { RoadPortDataLookup = RoadPortDataLookup }.ScheduleParallel(state.Dependency);
        state.Dependency = new UpdateRoadLaneNeighbours { RoadLaneDataLookup = RoadLaneDataLookup, RoadPortDataLookup = RoadPortDataLookup, RoadNodePortChildLookup = RoadNodePortChildLookup, RoadSegmentLaneLookup = RoadSegmentLaneLookup }.Schedule(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    public ComponentLookup<RoadLaneData> RoadLaneDataLookup;
    public ComponentLookup<RoadPortData> RoadPortDataLookup;
    public BufferLookup<RoadNodePortChild> RoadNodePortChildLookup;
    public BufferLookup<RoadSegmentLane> RoadSegmentLaneLookup;
}