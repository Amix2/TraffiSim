using Unity.Entities;
using UnityEngine;

public partial class rgDocument : MonoBehaviour
{
    public GameObject LaneNodePrefabGO;
    public GameObject LanePrefabGO;
    public GameObject SegmentNodePrefabGO;


    public partial class Baker : Baker<rgDocument>
    {
        public override void Bake(rgDocument authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            Entity RoadManager = CreateAdditionalEntity(TransformUsageFlags.None, false, "RoadManager");

            AddComponent(entity, new rgDocumentC
            {
                LaneNodePrefab = GetEntity(authoring.LaneNodePrefabGO, TransformUsageFlags.None),
                LanePrefab = GetEntity(authoring.LanePrefabGO, TransformUsageFlags.None),
                SegmentNodePrefab = GetEntity(authoring.SegmentNodePrefabGO, TransformUsageFlags.None),
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
    public Entity LaneNodePrefab;
    public Entity LanePrefab;
    public Entity SegmentNodePrefab;
    public Entity RoadManager;

    public static rgDocumentC GetSingletonValue(EntityManager entityManager)
    {
        var query = entityManager.CreateEntityQuery(typeof(rgDocumentC));
        return query.GetSingleton<rgDocumentC>();
    }
}