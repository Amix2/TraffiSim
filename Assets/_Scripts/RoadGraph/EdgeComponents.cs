using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct rgEdge : IComponentData
{
    public Entity Node1;
    public Entity Node2;
}

public struct rgEdgePosiotions : IComponentData
{
    public float3 Pos1;
    public float3 Pos2;
}

public struct ClosestRoadHit
{
    public float3 RoadPosition;
    public float RoadFract;
    public Entity Edge;

    public static ClosestRoadHit Null => new ClosestRoadHit()
    {
        RoadPosition = new float3(float.MaxValue, float.MaxValue, float.MaxValue),
        RoadFract = float.MaxValue,
        Edge = Entity.Null
    };

    public bool IsNull => RoadPosition.x == float.MaxValue;
}

public readonly partial struct rgEdgeAspect : IAspect
{
    public readonly Entity Entity;
    private readonly RefRO<rgEdge> Edge;
    private readonly RefRO<rgEdgePosiotions> Positions;


    public Entity NodeA => Edge.ValueRO.Node1;
    public Entity NodeB => Edge.ValueRO.Node2;
    public float3 PosA => Positions.ValueRO.Pos1;
    public float3 PosB => Positions.ValueRO.Pos2;
    public float3 RoadDir => PosB - PosA;
    public float EdgeLength => (PosB - PosA).length();
    public ClosestRoadHit GetClosestPoint(float3 P)
    {
        float3 A = PosA;
        float3 B = PosB;
        float edgeLen = (B - A).length();
        if (edgeLen == 0)
            return ClosestRoadHit.Null;
        float3 dir = RoadDir.norm();
        float x = math.dot(dir, P - A);

        float RoadFract = x / edgeLen;
        RoadFract = math.clamp(RoadFract, 0, 1);
        return new ClosestRoadHit()
        {
            RoadPosition = A + RoadFract * edgeLen * dir,
            RoadFract = RoadFract,
            Edge = Entity
        };
    }
}