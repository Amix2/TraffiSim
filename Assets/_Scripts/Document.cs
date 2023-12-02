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
            AddSharedComponentManaged(entity, new DocumentTool
            {
                Tool = null
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

public struct DocumentTool : ISharedComponentData, IEquatable<DocumentTool>
{
    public ITool Tool;

    public bool Equals(DocumentTool other)
    {
        if (Tool == null)
            return false;
        return Tool.Equals(other.Tool);
    }

    public override int GetHashCode()
    {
        return Tool != null ? Tool.GetHashCode() : 0;
    }
}

public readonly partial struct DocumentAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<DocumentComponent> DocumentComponent;

    public Entity VehiclePrefab => DocumentComponent.ValueRO.VehiclePrefab;
}