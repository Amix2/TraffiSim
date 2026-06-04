using Unity.Entities;
using UnityEngine;

public partial class rgDocument : MonoBehaviour
{
    public GameObject NodePrefabGO;
    public GameObject NodeVisualizerPrefabbGO;

    public GameObject LanePrefabGO;
    public GameObject LaneVisualizerPrefabbGO;

    public partial class Baker : Baker<rgDocument>
    {
        public override void Bake(rgDocument authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            Entity RoadManager = CreateAdditionalEntity(TransformUsageFlags.None, false, "RoadManager");

            AddComponent(entity, new rgDocumentC
            {
                NodePrefab = GetEntity(authoring.NodePrefabGO, TransformUsageFlags.None),
                NodeVisualizerPrefab = GetEntity(authoring.NodeVisualizerPrefabbGO, TransformUsageFlags.Dynamic),
                LanePrefab = GetEntity(authoring.LanePrefabGO, TransformUsageFlags.None),
                LaneVisualizerPrefab = GetEntity(authoring.LaneVisualizerPrefabbGO, TransformUsageFlags.Dynamic),
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
    public Entity NodeVisualizerPrefab;
    public Entity LanePrefab;
    public Entity LaneVisualizerPrefab;
    public Entity RoadManager;

    public static rgDocumentC GetSingletonValue(EntityManager entityManager)
    {
        var query = entityManager.CreateEntityQuery(typeof(rgDocumentC));
        return query.GetSingleton<rgDocumentC>();
    }
}