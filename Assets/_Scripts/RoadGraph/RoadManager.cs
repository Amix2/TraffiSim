using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct rgRoadManager : IComponentData
{
}

public struct rgRoadNodes : IBufferElementData
{
    public Entity Node;

    public static implicit operator Entity(rgRoadNodes val) => val.Node;
    public static explicit operator rgRoadNodes(Entity val) => new() { Node = val };
}

public struct rgRoadEdges : IBufferElementData
{
    public Entity Edge;

    public static implicit operator Entity(rgRoadEdges val) => val.Edge;
    public static explicit operator rgRoadEdges(Entity val) => new() { Edge = val };
}

public readonly partial struct rgRoadManagerAspect : IAspect
{
    public readonly Entity DocumentEntity;

    private readonly DynamicBuffer<rgRoadEdges> Edges;
    private readonly DynamicBuffer<rgRoadNodes> Nodes;

    public ClosestRoadHit GetClosestRoad(float3 position, in rgEdgeAspect.Lookup rgEdges)
    {
        ClosestRoadHit closestRoadHit = ClosestRoadHit.Null;
        float closestDistSq = float.MaxValue;

        for (int i = 0; i < Edges.Length; i++)
        {
            ClosestRoadHit hit = rgEdges[Edges[i]].GetClosestPoint(position);
            float hitDistSq = (position - hit.RoadPosition).lengthsq();
            if (hitDistSq < closestDistSq)
            {
                closestDistSq = hitDistSq;
                closestRoadHit = hit;
            }
        }

        return closestRoadHit;
    }

    public NativeArray<rgRoadEdges> GetEdges => Edges.AsNativeArray();
    public NativeArray<rgRoadNodes> GetNodes => Nodes.AsNativeArray();
}