using Unity.Entities;
using UnityEngine;

public partial class rgDocument : MonoBehaviour
{
    public GameObject PortPrefabGO;
    public GameObject LanePrefabGO;
    public GameObject NodePrefabGO;
    public GameObject SegmentPrefabGO;
    public GameObject RoadSurfacePrefab;

    public partial class Baker : Baker<rgDocument>
    {
        public override void Bake(rgDocument authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            //Entity RoadManager = CreateAdditionalEntity(TransformUsageFlags.None, false, "RoadManager");

            AddComponent(entity, new rgDocumentC
            {
                PortPrefab = GetEntity(authoring.PortPrefabGO, TransformUsageFlags.None),
                LanePrefab = GetEntity(authoring.LanePrefabGO, TransformUsageFlags.None),
                NodePrefab = GetEntity(authoring.NodePrefabGO, TransformUsageFlags.None),
                SegmentPrefab = GetEntity(authoring.SegmentPrefabGO, TransformUsageFlags.Dynamic),
                RoadSurfacePrefab = GetEntity(authoring.RoadSurfacePrefab, TransformUsageFlags.Dynamic),
                //RoadManager = RoadManager
            });

            //AddBuffer<rgRoadNodes>(RoadManager);
            //AddBuffer<rgRoadEdges>(RoadManager);
            //AddComponent<rgRoadManager>(RoadManager);
        }
    }
}

public struct rgDocumentC : IComponentData
{
    public Entity PortPrefab;
    public Entity LanePrefab;
    public Entity NodePrefab;
    public Entity SegmentPrefab;
    public Entity RoadManager;

    public Entity RoadSurfacePrefab;

    public static rgDocumentC GetSingletonValue(EntityManager entityManager)
    {
        var query = entityManager.CreateEntityQuery(typeof(rgDocumentC));
        return query.GetSingleton<rgDocumentC>();
    }

    public enum Prefab
    {
        Port = 0,
        Lane = 1,
        Node = 2,
        Segment = 3,
    }

    public readonly Entity GetPrefab(Prefab prefab)
    {
        return prefab switch
        {
            Prefab.Port => PortPrefab,
            Prefab.Lane => LanePrefab,
            Prefab.Node => NodePrefab,
            Prefab.Segment => SegmentPrefab,
            _ => throw new System.Exception(),
        };
    }
}