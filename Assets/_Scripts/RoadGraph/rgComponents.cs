using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;




public readonly partial struct rgNodeAspect : IAspect
{
    public readonly Entity Entity;
    private readonly RefRW<LocalTransform> NodePos;
}


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

public readonly partial struct rgEdgeAspect : IAspect
{
    public readonly Entity Entity;
    private readonly RefRW<rgEdge> Edge;
    private readonly RefRW<rgEdgePosiotions> Positions;

}

