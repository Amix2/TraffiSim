using System;
using System.Collections.Generic;
using Unity.Mathematics;

internal class RoadLaneNode : JsonSerializable
{
    public Guid Id;
    public List<float> Position;
    public float3 PositionFl3 => Position.Count == 2 ? new float3(Position[0], 0, Position[1]) : new float3(Position[0], Position[1], Position[2]);
}

internal class RoadLane : JsonSerializable
{
    public Guid Id;
    public Guid StartNode, EndNode;
}

class RoadSegmentNode : JsonSerializable
{
    public Guid Id;
    public List<Guid> LaneNodes;
}

internal class RoadBlueprint : JsonSerializable
{
    public List<RoadLaneNode> RoadLaneNodes = new();
    [JsonOptional]
    public List<RoadSegmentNode> RoadSegmentNodes = new();
    public List<RoadLane> RoadLanes = new();
}