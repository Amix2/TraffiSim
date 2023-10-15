using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.EntitiesJournaling;

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
