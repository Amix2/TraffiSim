using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class MeshWithExtAuthoring : MonoBehaviour
{
    public Optional<Color> Color;
    public Optional<float2> TextureTiling;

    public class Baker : Baker<MeshWithExtAuthoring>
    {
        public override void Bake(MeshWithExtAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.Dynamic);
            if(authoring.Color)
            {
                var c = (Vector4)authoring.Color.Value.linear;
                AddComponent(Entity, new URPMaterialPropertyBaseColor
                {
                    Value = new float4(c.x, c.y, c.z, c.w)
                });
            }
            if(authoring.TextureTiling)
            {
                AddComponent(Entity, new MaterialPropertyTextureTiling
                {
                    Value = authoring.TextureTiling.Value
                });
            }
        }
    }
}