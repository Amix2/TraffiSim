using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class rgSpawnHelper
{
    public static Entity SpawnRoadPrefab(EntityManager entityManager, EntityCommandBuffer ecb, rgDocumentC.Prefab prefab, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = entityManager.Instantiate(document.GetPrefab(prefab));
        entityManager.SetName(entity, prefab.ToString() + "_" + name);
        return entity;
    }

    public static void SetupRoadSegment(EntityManager entityManager, EntityCommandBuffer ecb, Entity RoadSegEnt, NativeArray<Entity> Nodes, NativeArray<Entity> Lanes)
    {
        entityManager.GetBuffer<RoadSegmentNode>(RoadSegEnt).Reinterpret<Entity>().AddRange(Nodes);
        entityManager.GetBuffer<RoadSegmentLane>(RoadSegEnt).Reinterpret<Entity>().AddRange(Lanes);
    }

    public struct SetupRoadPortData
    {
        public Entity RoadPortEnt;
        public Entity Parent;
        public float4x4 transform;
        public NativeArray<Entity> InputLanes;
        public NativeArray<Entity> OutputLanes;
    }

    public static void SetupRoadPort(EntityManager entityManager, EntityCommandBuffer ecb, Entity RoadPortEnt, SetupRoadPortData data)
    {
        RoadPortData RoadPortData = new()
        {
            //Transform = data.transform,
            ParentNodeEnt = data.Parent,
        };
        entityManager.SetComponentData(RoadPortEnt, RoadPortData);
        entityManager.GetBuffer<RoadPortInput>(RoadPortEnt).Reinterpret<Entity>().AddRange(data.InputLanes);
        entityManager.GetBuffer<RoadPortOutput>(RoadPortEnt).Reinterpret<Entity>().AddRange(data.OutputLanes);
    }

    public static Entity SpawnRoadSegmentNode(EntityManager entityManager, EntityCommandBuffer ecb, NativeArray<Entity> LaneNodes, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = entityManager.Instantiate(document.NodePrefab);
        //entityManager.SetName(entity, "RoadSegmentNode_" + name);
        //entityManager.GetBuffer<RoadSegmentNodeElements>(entity).Reinterpret<Entity>().AddRange(LaneNodes);
        //entityManager.SetComponentEnabled<RoadSegmentNodeUpdateChildNodes>(entity, true);

        //AttachRoadVisualizerParent(entityManager, ecb, entity);

        return entity;
    }

    private static void AttachRoadVisualizerParent(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity)
    {
        //var kids = entityManager.GetBuffer<LinkedEntityGroup>(entity);
        //foreach (LinkedEntityGroup linkedEntity in kids)
        //{
        //    Entity linkedEnt = linkedEntity.Value;
        //    if (entityManager.HasComponent<RoadVisualizerParent>(linkedEnt))
        //    {
        //        entityManager.SetComponentData(linkedEnt, new RoadVisualizerParent { ParentEnt = entity });
        //        entityManager.SetName(linkedEnt, entityManager.GetName(entity) + "_Visualizer");
        //    }
        //}
    }
}