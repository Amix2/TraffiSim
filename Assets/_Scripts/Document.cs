using System;
using Unity.Entities;
using UnityEngine;

public class Document : MonoBehaviour
{
    public GameObject VehiclePrefabGO;

    public Shader DefaultShader;

    public class Baker : Baker<Document>
    {
        public override void Bake(Document authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new DocumentComponent
            {
                VehiclePrefab = GetEntity(authoring.VehiclePrefabGO, TransformUsageFlags.Renderable),
            });
            AddSharedComponentManaged(entity, new DocumentSharedComponent
            {
                DefaultShader = authoring.DefaultShader
            });
        }
    }
}

public struct DocumentComponent : IComponentData
{
    public Entity VehiclePrefab;
}

public struct DocumentSharedComponent : ISharedComponentData, IEquatable<DocumentSharedComponent>
{
    public Shader DefaultShader;

    public bool Equals(DocumentSharedComponent other)
    {
        if (!DefaultShader)
            return false;
        return DefaultShader.Equals(other.DefaultShader);
    }
    public override int GetHashCode()
    {
        return DefaultShader.GetHashCode();
    }
}

public readonly partial struct DocumentAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<DocumentComponent> DocumentComponent;
   // private readonly DocumentSharedComponent DocumentSharedComponent;

    public Entity VehiclePrefab => DocumentComponent.ValueRO.VehiclePrefab;
    //public Shader DefaultShader => DocumentSharedComponent.DefaultShader;

}