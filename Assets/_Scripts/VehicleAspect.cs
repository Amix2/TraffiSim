using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public readonly partial struct VehicleAspect : IAspect
{
    public readonly Entity Entity;
    public readonly RefRO<VehicleTag> Tag;
    public readonly RefRW<Velocity> Velocity;
    public readonly RefRW<DestinationPosition> DestinationPosition;
    public readonly RefRW<Acceleration> Acceleration;
    public readonly RefRW<MaxVelocity> MaxVelocity;
    public readonly RefRW<LastStepPosition> LastStepPosition;
    public readonly RefRW<LastStepOccupiedEdge> LastStepOccupiedEdge;
    public readonly RefRW<LocalTransform> LocalTransform;
    public readonly DynamicBuffer<PathBuffer> PathBuffer;

    public bool IsAtDestination(float rangeSq) { return (DestinationPosition.ValueRO.Value - LocalTransform.ValueRO.Position).lengthsq() < rangeSq; }

    public OBB GetObb() { return new OBB(LocalTransform.ValueRO.Position, new float3(10, 5, 5), LocalTransform.ValueRO.Rotation); }
}