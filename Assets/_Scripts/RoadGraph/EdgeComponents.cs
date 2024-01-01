using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;

public struct rgEdge : IComponentData
{
    public Entity Start;
    public Entity End;
}

public struct rgEdgeOccupant : IBufferElementData
{
    public Entity Entity;
    public float Fract;
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

    public readonly bool IsNull => RoadPosition.x == float.MaxValue;
}

public readonly partial struct rgEdgeAspect : IAspect
{
    public readonly Entity Entity;
    private readonly RefRO<rgEdge> Edge;
    private readonly RefRO<rgEdgePosiotions> Positions;
    private readonly DynamicBuffer<rgEdgeOccupant> OccupantsDB;
    private readonly NativeArray<rgEdgeOccupant> OccupantsNA => OccupantsDB.AsNativeArray();

    public Entity Start => Edge.ValueRO.Start;
    public Entity End => Edge.ValueRO.End;
    public float3 StartPos => Positions.ValueRO.Pos1;
    public float3 EndPos => Positions.ValueRO.Pos2;
    public float3 RoadDir => EndPos - StartPos;
    public float EdgeLength => (EndPos - StartPos).length();

    public void RemoveOccupant(Entity occupant)
    {
        var Occupants = OccupantsNA;
        for (int i = 0; i< Occupants.Length; i++)
        {
            if (Occupants[i].Entity == occupant)
                Occupants[i] = new rgEdgeOccupant { Entity = Entity.Null, Fract = float.MaxValue };
        }
    }

    public void AddOccupant(Entity occupant)
    {
        OccupantsDB.Add(new rgEdgeOccupant { Entity = occupant, Fract = float.MaxValue });
    }

    public ClosestRoadHit GetClosestPoint(float3 P)
    {
        float3 A = StartPos;
        float edgeLen = EdgeLength;
        if (edgeLen == 0)
            return ClosestRoadHit.Null;
        float3 dir = RoadDir.norm();

        float RoadFract = GetRoadFract(P);
        return new ClosestRoadHit()
        {
            RoadPosition = A + RoadFract * edgeLen * dir,
            RoadFract = RoadFract,
            Edge = Entity
        };
    }

    public float GetRoadFract(float3 P)
    {
        float3 A = StartPos;
        float3 B = EndPos;
        float edgeLen = EdgeLength;
        if (edgeLen == 0)
            return float.MaxValue;
        float3 dir = RoadDir.norm();
        float x = math.dot(dir, P - A);

        float RoadFract = x / edgeLen;
        RoadFract = math.clamp(RoadFract, 0, 1);
        return RoadFract;
    }
}