using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct PathBuffer : IBufferElementData
{
    public Entity Target;
    public Entity EdgeEnt;
    public float3 Position;
}

public struct PositionTimePoint : IBufferElementData
{
    public float3 Position;
    public quaternion Orientation;
    public float Time;
    public float Distance;
}

public struct Velocity : IComponentData
{
    public float Value;

    public static implicit operator float(Velocity vel) => vel.Value;

    public static implicit operator Velocity(float val) => new() { Value = val };
}

public struct MaxVelocity : IComponentData
{
    public float Value;

    public static implicit operator float(MaxVelocity vel) => vel.Value;

    public static implicit operator MaxVelocity(float val) => new() { Value = val };
}

public struct Acceleration : IComponentData
{
    public float Value;

    public static implicit operator float(Acceleration vel) => vel.Value;

    public static implicit operator Acceleration(float val) => new() { Value = val };
}

public struct DestinationPosition : IComponentData
{
    public float3 Value;

    public static implicit operator float3(DestinationPosition val) => val.Value;

    public static implicit operator DestinationPosition(float3 val) => new() { Value = val };
}

public struct VehicleTag : IComponentData
{ }

public struct LastStepPosition : IComponentData
{
    public float3 Value;

    public static implicit operator float3(LastStepPosition val) => val.Value;

    public static implicit operator LastStepPosition(float3 val) => new() { Value = val };
}

public struct LastStepOccupiedEdge : IComponentData
{
    public Entity Value;

    public static implicit operator Entity(LastStepOccupiedEdge val) => val.Value;

    public static implicit operator LastStepOccupiedEdge(Entity val) => new() { Value = val };
}

public struct LoadVehiclesFromJsonFile : IComponentData
{
    public FixedString512Bytes fileName;
}

public struct LoadVehiclesFromTextJson : ISharedComponentData, IEquatable<LoadVehiclesFromTextJson>
{
    public string jsonText;

    public bool Equals(LoadVehiclesFromTextJson other)
    {
        return jsonText == other.jsonText;
    }

    public override int GetHashCode()
    {
        return jsonText.GetHashCode();
    }
}

public struct SaveVehiclesFromJson : IComponentData
{
    public FixedString512Bytes fileName;
}

public struct FutureCollisionDistanceC : IComponentData
{
    public float Value;

    public static implicit operator float(FutureCollisionDistanceC val) => val.Value;
    public static implicit operator FutureCollisionDistanceC(float val) => new() { Value = val };
}

public struct Priority : IComponentData
{
    public double Value;

    public static implicit operator double(Priority val) => val.Value;
    public static implicit operator Priority(double val) => new() { Value = val };
}