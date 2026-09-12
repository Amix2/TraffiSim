using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct float3Pair
{
    public float3 A;
    public float3 B;
}

public class UnityObjectString : UnityEngine.Object
{
    public string Value;
    public static implicit operator string(UnityObjectString v) { return v.Value; }
    public UnityObjectString(string value) : base() { Value = value; }
}