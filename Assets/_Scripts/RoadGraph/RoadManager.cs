using Unity.Entities;
using Unity.Mathematics;

public struct rgRoadNodes : IBufferElementData
{
    public Entity Node;
}

public struct rgRoadEdges : IBufferElementData
{
    public Entity Edge;
}

public struct ClosestRoadHit
{
    public float3 RoadPosition;
    public Entity Edge;
}

public readonly partial struct rgRoadManagerAspect : IAspect
{
    public readonly Entity DocumentEntity;

    private readonly DynamicBuffer<rgRoadEdges> Edges;
    private readonly DynamicBuffer<rgRoadNodes> Nodes;

    //public ClosestRoadHit GetClosestRoad(float3 position)
    //{
    //}
}