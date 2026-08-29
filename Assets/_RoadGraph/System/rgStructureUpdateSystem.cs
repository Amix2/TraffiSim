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
    [ReadOnly] public RoadPortAspect.Lookup PortLookup;

    public void Execute(Entity entity,
            RefRO<RoadLaneData> roadLaneData, DynamicBuffer<RoadLanePoint> roadLanePoints, EnabledRefRW<RoadLaneUpdatePoints> update)
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

[BurstCompile]
public partial struct UpdateRoadLaneNeighbours : IJobEntity
{
    [ReadOnly] public RoadPortAspect.Lookup PortLookup;
    [ReadOnly] public RoadSegmentAspect.Lookup SegmentLookup;
    [ReadOnly] public RoadNodeAspect.Lookup NodeLookup;
    [ReadOnly] public RoadLaneAspect.Lookup LaneLookup;


    RoadLaneAspect FindLaneWithPort(DynamicBuffer<RoadLaneEnt> lanes, in RoadPortEnt port)
    {
        foreach (RoadLaneEnt lane in lanes)
        {
            RoadLaneAspect laneAspect = LaneLookup[lane];
            if (laneAspect.Data.ValueRO.EndPortEnt == port || laneAspect.Data.ValueRO.StartPortEnt == port)
                return laneAspect;
        }
        return new();
    }

    public void Execute(Entity entity,
            RefRO<RoadLaneData> data, DynamicBuffer<RoadLaneNeighbour> neighbours, EnabledRefRW<RoadLaneUpdateNeighbours> update)
    {
        RoadSegmentAspect parentSegment = SegmentLookup[data.ValueRO.Parent];
        DynamicBuffer<RoadLaneEnt> siblings = parentSegment.ChildLanes.Reinterpret<RoadLaneEnt>();
        RoadPortAspect endPort = PortLookup[data.ValueRO.EndPortEnt];
        RoadNodeAspect endNode = NodeLookup[endPort.Parent];
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
        if (NeiPortID > 0 && NeiPortID < portsAtEndNode.Length)
        {
            RoadLaneAspect neiEnt = FindLaneWithPort(siblings, portsAtEndNode[NeiPortID]);
            if (neiEnt.Entity != Entity.Null)
            {
                bool TheSameDir = neiEnt.Data.ValueRO.StartPortEnt == data.ValueRO.StartPortEnt;
                neighbours.Add(new RoadLaneNeighbour { Entity = neiEnt.Entity, TheSameDirection = TheSameDir});
            }
        }
        NeiPortID = MyIndexInNode + 1;
        if (NeiPortID > 0 && NeiPortID < portsAtEndNode.Length)
        {
            RoadLaneAspect neiEnt = FindLaneWithPort(siblings, portsAtEndNode[NeiPortID]);
            if (neiEnt.Entity != Entity.Null)
            {
                bool TheSameDir = neiEnt.Data.ValueRO.StartPortEnt == data.ValueRO.StartPortEnt;
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
        RoadPortAspectLookup.Initialize(ref state);
        RoadPortAspectLookup.LocalTransformLookup.Request(ref state, true);
        RoadPortAspectLookup.RoadPortDataLookup.Request(ref state, true);

        RoadSegmentAspectLookup.Initialize(ref state);
        RoadSegmentAspectLookup.RoadSegmentLaneLookup.Request(ref state, true);

        RoadNodeAspectLookup.Initialize(ref state);
        RoadNodeAspectLookup.RoadNodePortChildLookup.Request(ref state, true);

        RoadLaneAspectLookup.Initialize(ref state);
        RoadLaneAspectLookup.RoadLaneDataLookup.Request(ref state, true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RoadPortAspectLookup.Update(ref state);
        RoadSegmentAspectLookup.Update(ref state);
        RoadNodeAspectLookup.Update(ref state);
        RoadLaneAspectLookup.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.Dependency = new UpdatePortsInOutBuffers { }.ScheduleParallel(state.Dependency);
        state.Dependency = new UpdateRoadLanePoints { PortLookup = RoadPortAspectLookup }.ScheduleParallel(state.Dependency);
        state.Dependency = new UpdateRoadLaneNeighbours { PortLookup = RoadPortAspectLookup, SegmentLookup = RoadSegmentAspectLookup, NodeLookup = RoadNodeAspectLookup, LaneLookup = RoadLaneAspectLookup }.Schedule(state.Dependency);

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    public RoadPortAspect.Lookup RoadPortAspectLookup;
    public RoadSegmentAspect.Lookup RoadSegmentAspectLookup;
    public RoadNodeAspect.Lookup RoadNodeAspectLookup;
    public RoadLaneAspect.Lookup RoadLaneAspectLookup;
}