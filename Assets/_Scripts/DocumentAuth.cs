using Unity.Entities;
using UnityEngine;

public class DocumentAuth : MonoBehaviour
{
    public GameObject VehiclePrefabGO;

    public class Baker : Baker<DocumentAuth>
    {
        public override void Bake(DocumentAuth authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new DocumentComponent
            {
                VehiclePrefab = GetEntity(authoring.VehiclePrefabGO, TransformUsageFlags.Renderable),
            });
        }
    }
}
