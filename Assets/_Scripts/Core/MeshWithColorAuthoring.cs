using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class MeshWithColorAuthoring : MonoBehaviour
{
    public Color Color = Color.red;

    public class Baker : Baker<MeshWithColorAuthoring>
    {
        public override void Bake(MeshWithColorAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.Dynamic);
            var c = (Vector4)authoring.Color.linear;
            AddComponent(Entity, new URPMaterialPropertyBaseColor
            {
                Value = new float4(c.x, c.y, c.z, c.w)
            });
        }
    }
}