using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct rgLoadRoadFromJson : IComponentData
{
    public FixedString512Bytes fileName;
}

public struct rgSpawnNodeOrder : IComponentData
{
    public float3 position;
}