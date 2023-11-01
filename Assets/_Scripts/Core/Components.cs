using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PathBuffer : IBufferElementData
{
    public Entity Target;
    public float3 Position;
}

public struct Velocity : IComponentData
{
    public float Value;

    public static implicit operator float(Velocity vel) => vel.Value;
    public static explicit operator Velocity(float val) => new() { Value = val};
}

public struct MaxVelocity : IComponentData
{
    public float Value;

    public static implicit operator float(MaxVelocity vel) => vel.Value;
    public static explicit operator MaxVelocity(float val) => new() { Value = val };
}

public struct Acceleration : IComponentData
{
    public float Value;

    public static implicit operator float(Acceleration vel) => vel.Value;
    public static explicit operator Acceleration(float val) => new() { Value = val };
}


public struct TargetPosition : IComponentData
{
    public float3 Value;

    public static implicit operator float3(TargetPosition val) => val.Value;
    public static explicit operator TargetPosition(float3 val) => new() { Value = val };
}