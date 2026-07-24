using System;
using System.Collections.Generic;
using Unity.Mathematics;

internal class RoadNode : JsonSerializable
{
    public Guid Id;
}

internal class RoadSegment : JsonSerializable
{
    public Guid Id;
    [JsonOptional]
    public List<Guid> Nodes = new();
}

internal class RoadPort : JsonSerializable
{
    public Guid Id;
    public Guid Parent;
    public List<float> Position;
    public float3 PositionFl3 => Position.Count == 2 ? new float3(Position[0], 0, Position[1]) : new float3(Position[0], Position[1], Position[2]);
}


internal class RoadLane : JsonSerializable
{
    public Guid Id;
    public Guid StartPort, EndPort;
    public Guid Parent;
}



internal class RoadBlueprint : JsonSerializable
{
    [JsonOptional]
    public List<RoadNode> RoadNodes = new();
    [JsonOptional]
    public List<RoadSegment> RoadSegments = new();
    [JsonOptional]
    public List<RoadPort> RoadPorts = new();
    [JsonOptional]
    public List<RoadLane> RoadLanes = new();

}