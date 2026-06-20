using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

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

public struct LoadVehiclesFromJsonFile : IComponentData
{
    public FixedString512Bytes fileName;
}

public struct VehiclesJsonBlob
{
    public BlobString Json;

    public static BlobAssetReference<VehiclesJsonBlob> Create(string jsonText)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref VehiclesJsonBlob root = ref builder.ConstructRoot<VehiclesJsonBlob>();

        builder.AllocateString(ref root.Json, jsonText);

        var blob = builder.CreateBlobAssetReference<VehiclesJsonBlob>(Allocator.Persistent);
        builder.Dispose();

        return blob;
    }
}

public struct LoadVehiclesFromTextJson : IComponentData
{
    public BlobAssetReference<VehiclesJsonBlob> jsonText;
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

public struct FactoryInput : IComponentData
{
    public float3 Value;

    public static implicit operator float3(FactoryInput val) => val.Value;

    public static implicit operator FactoryInput(float3 val) => new() { Value = val };
}

public struct FactoryOutput : IComponentData
{
    public float3 Value;

    public static implicit operator float3(FactoryOutput val) => val.Value;

    public static implicit operator FactoryOutput(float3 val) => new() { Value = val };
}

[MaterialProperty("_TextureTiling", -1)]
public struct MaterialPropertyTextureTiling : IComponentData, IQueryTypeParameter
{
    public float2 Value;
}