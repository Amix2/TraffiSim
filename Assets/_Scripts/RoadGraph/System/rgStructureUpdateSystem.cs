using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[WithAll(typeof(RoadLaneNodeUpdateInOutBuffers))]
public partial struct UpdateNodesInOutBuffers : IJobEntity
{

    public void Execute(ref DynamicBuffer<RoadLaneNodeInput> inputs, ref DynamicBuffer<RoadLaneNodeOutput> outputs)
    {
    }
}

public partial struct rgStructureUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
    
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    
    }
}