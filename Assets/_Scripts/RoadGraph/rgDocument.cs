using Unity.Entities;
using UnityEngine;

public partial class rgDocument : MonoBehaviour
{
    public GameObject NodePrefabGO;

    public partial class Baker : Baker<rgDocument>
    {
        public override void Bake(rgDocument authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            Entity RoadManager = CreateAdditionalEntity(TransformUsageFlags.None, false, "RoadManager");

            AddComponent(entity, new rgDocumentC
            {
                NodePrefab = GetEntity(authoring.NodePrefabGO, TransformUsageFlags.Renderable),
                RoadManager = RoadManager
            });

            //AddBuffer<rgRoadNodes>(RoadManager);
            //AddBuffer<rgRoadEdges>(RoadManager);
            //AddComponent<rgRoadManager>(RoadManager);
        }
    }
}

public struct rgDocumentC : IComponentData
{
    public Entity NodePrefab;
    public Entity RoadManager;
}
