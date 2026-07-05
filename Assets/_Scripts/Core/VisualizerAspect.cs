using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Graphs;
public struct VisualizerAspect
{
    public struct Lookup
    {
        public LookupSlot<LocalTransform> LocalTransformLookup;
        public LookupSlot<PostTransformMatrix> PostTransformMatrixLookup;
        public LookupSlot<MaterialPropertyTextureTiling> MaterialPropertyTextureTilingLookup;

        public void Initialize(ref SystemState state)
        {
            LocalTransformLookup.Initialize(ref state);
            PostTransformMatrixLookup.Initialize(ref state);
            MaterialPropertyTextureTilingLookup.Initialize(ref state);
        }

        /// SystemBase / managed system variant.
        public void Initialize(ComponentSystemBase system)
        {
            LocalTransformLookup.Initialize(system);
            PostTransformMatrixLookup.Initialize(system);
            MaterialPropertyTextureTilingLookup.Initialize(system);
        }

        public void Update(ref SystemState state)
        {
            LocalTransformLookup.Update(ref state);
            PostTransformMatrixLookup.Update(ref state);
            MaterialPropertyTextureTilingLookup.Update(ref state);
        }
        public void Update(SystemBase system)
        {
            LocalTransformLookup.Update(system);
            PostTransformMatrixLookup.Update(system);
            MaterialPropertyTextureTilingLookup.Update(system);

        }
        public VisualizerAspect this[Entity e] => new VisualizerAspect
        {
            Entity = e,
            LocalTransformRW = LocalTransformLookup.BindRW(e),
            PostTransformMatrixRW = PostTransformMatrixLookup.BindRWOptional(e),
            MaterialPropertyTextureTilingRW = MaterialPropertyTextureTilingLookup.BindRWOptional(e)
        };
    }
    public Entity Entity;
    public RefRW<LocalTransform> LocalTransformRW;
    public RefRW<PostTransformMatrix> PostTransformMatrixRW;
    public RefRW<MaterialPropertyTextureTiling> MaterialPropertyTextureTilingRW;

    public void SetTextureTiling(float x, float y = 1)
    {
        MaterialPropertyTextureTilingRW.ValueRW.Value = new float2(x, y); 
    }
    public void SetPositionRotation(float3 position, quaternion rotation)
    {
        LocalTransformRW.ValueRW = LocalTransform.FromPositionRotation(position, rotation);
    }
    public void SetNonUniformScale(float x, float y, float z)
    {
        PostTransformMatrixRW.ValueRW.Value = float4x4.Scale(x,y,z);
    }

}