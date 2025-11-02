using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
public struct rgEdge : IComponentData
{
    public Entity Start;
    public Entity End;
    public bool BothWays;
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

public struct rgEdgeAspect
{
    public Entity Entity;
    private RefRO<rgEdge> Edge;
    private RefRO<rgEdgePosiotions> Positions;
    private DynamicBuffer<rgEdgeOccupant> OccupantsDB;

    public static implicit operator rgEdgeAspect(Entity val) => new() { Entity = val };
    public static rgEdgeAspect Make(Entity Entity, BufferLookup<rgEdgeOccupant> lookup)
    {
        rgEdgeAspect edgeAspect = new()
        {
            Entity = Entity,
            OccupantsDB = lookup[Entity],
        };
        return edgeAspect;
    }
    public static rgEdgeAspect Make(Entity Entity, ComponentLookup<rgEdgePosiotions> lookup)
    {
        rgEdgeAspect edgeAspect = new()
        {
            Entity = Entity,
            Positions = lookup.GetRefRO(Entity),
        };
        return edgeAspect;
    }
    public static rgEdgeAspect Make(Entity Entity, ComponentLookup<rgEdge> rgEdgeLookup, ComponentLookup<rgEdgePosiotions> rgEdgePosiotionsLookup)
    {
        rgEdgeAspect edgeAspect = new()
        {
            Entity = Entity,
            Edge = rgEdgeLookup.GetRefRO(Entity),
            Positions = rgEdgePosiotionsLookup.GetRefRO(Entity)
        };
        return edgeAspect;
    }

    public readonly Entity Start => Edge.ValueRO.Start;
    public readonly Entity End => Edge.ValueRO.End;
    public readonly float3 StartPos => Positions.ValueRO.Pos1;
    public readonly float3 EndPos => Positions.ValueRO.Pos2;
    public readonly float3 RoadDir => EndPos - StartPos;
    public readonly float EdgeLength => (EndPos - StartPos).length();

    public void RemoveOccupant(Entity occupant)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Unity.Assertions.Assert.IsTrue(OccupantsDB.IsCreated);
#endif
        for (int i = 0; i < OccupantsDB.Length; i++)
        {
            if (OccupantsDB[i].Entity == occupant)
                OccupantsDB[i] = new rgEdgeOccupant { Entity = Entity.Null, Fract = float.MaxValue };
        }
    }
    public void AddOccupant(Entity occupant)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Unity.Assertions.Assert.IsTrue(OccupantsDB.IsCreated);
#endif
        OccupantsDB.Add(new rgEdgeOccupant { Entity = occupant, Fract = float.MaxValue });

    }

    public readonly ClosestRoadHit GetClosestPoint(float3 P)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Unity.Assertions.Assert.IsTrue(Positions.IsValid);
#endif

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

    public readonly float GetRoadFract(float3 P)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Unity.Assertions.Assert.IsTrue(Positions.IsValid);
#endif

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