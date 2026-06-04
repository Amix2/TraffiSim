using System;
using System.Collections.Generic;
using Unity.Mathematics;

internal class RoadNode : JsonSerializable
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

internal class RoadBlueprint : JsonSerializable
{
    public List<RoadNode> RoadNodes;
    public List<RoadLane> RoadLanes;
}