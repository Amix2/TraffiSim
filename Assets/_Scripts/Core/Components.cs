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