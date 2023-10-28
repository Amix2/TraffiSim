using Unity.Entities;
using UnityEngine;

public class Document : MonoBehaviour
{
    public GameObject VehiclePrefabGO;

    public class Baker : Baker<Document>
    {
        public override void Bake(Document authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new DocumentComponent
            {
                VehiclePrefab = GetEntity(authoring.VehiclePrefabGO, TransformUsageFlags.Renderable),
            });
        }
    }
}

public struct DocumentComponent : IComponentData
{
    public Entity VehiclePrefab;
}

public readonly partial struct DocumentAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<DocumentComponent> DocumentComponent;

    public Entity VehiclePrefab => DocumentComponent.ValueRO.VehiclePrefab;
}