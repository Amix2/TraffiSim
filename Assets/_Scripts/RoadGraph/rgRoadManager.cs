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


public struct rgRoadManagerAspect
{
    public Entity DocumentEntity;

    private DynamicBuffer<rgRoadEdges> Edges;
    private DynamicBuffer<rgRoadNodes> Nodes;

    static public rgRoadManagerAspect Make(Entity documentEntity, BufferLookup<rgRoadEdges, rgRoadNodes> bufferLookup)
    {
        rgRoadManagerAspect aspect = new()
        {
            DocumentEntity = documentEntity,
            Edges = bufferLookup.GetBuffer<rgRoadEdges>(documentEntity),
            Nodes = bufferLookup.GetBuffer<rgRoadNodes>(documentEntity),
        };
        return aspect;
    }
    public ClosestRoadHit GetClosestRoad(float3 position, ComponentLookup<rgEdgePosiotions> rgEdgePosiotionsLookup)
    {
        ClosestRoadHit closestRoadHit = ClosestRoadHit.Null;
        float closestDistSq = float.MaxValue;

        for (int i = 0; i < Edges.Length; i++)
        {
            ClosestRoadHit hit = rgEdgeAspect.Make(Edges[i], rgEdgePosiotionsLookup).GetClosestPoint(position);
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