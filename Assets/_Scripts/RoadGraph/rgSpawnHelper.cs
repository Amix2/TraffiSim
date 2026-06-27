using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class rgSpawnHelper
{
    public static Entity SpawnRoadLaneNode(EntityManager entityManager, EntityCommandBuffer ecb, float4x4 transform, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = entityManager.Instantiate(document.LaneNodePrefab);
        entityManager.SetComponentData(entity, new RoadLaneNodeData { Transform = transform });
        entityManager.SetName(entity, "RoadLaneNode_" + name);

        var kids = entityManager.GetBuffer<LinkedEntityGroup>(entity);

        foreach (LinkedEntityGroup linkedEntity in kids)
        {
            Entity linkedEnt = linkedEntity.Value;
            if (entityManager.HasComponent<RoadLaneNodeVisualizer>(linkedEnt))
            {
                entityManager.SetComponentData(linkedEnt, new RoadLaneNodeVisualizer { RoadLaneNodeEnt = entity });
                entityManager.SetName(linkedEnt, entityManager.GetName(entity) + "_Visualizer");
            }
        }

        return entity;
    }

    public static Entity SpawnRoadLane(EntityManager entityManager, EntityCommandBuffer ecb, Entity startNode, Entity endNode, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = entityManager.Instantiate(document.LanePrefab);
        entityManager.SetComponentData(entity, new RoadLaneData { StartNodeEnt = startNode, EndNodeEnt = endNode, LaneWidth = 3f });
        entityManager.SetName(entity, "RoadLane_" + name);

        var kids = entityManager.GetBuffer<LinkedEntityGroup>(entity);

        foreach (LinkedEntityGroup linkedEntity in kids)
        {
            Entity linkedEnt = linkedEntity.Value;
            if (entityManager.HasComponent<RoadLaneVisualizer>(linkedEnt))
            {
                entityManager.SetComponentData(linkedEnt, new RoadLaneVisualizer { RoadLaneEnt = entity });
                entityManager.SetName(linkedEnt, entityManager.GetName(entity) + "_Visualizer");
            }
        }

        entityManager.GetBuffer<RoadLaneNodeOutput>(startNode).Add(new RoadLaneNodeOutput { RoadLaneEnt = entity });
        entityManager.SetComponentEnabled<RoadLaneNodeUpdateInOutBuffers>(endNode, true);
        entityManager.GetBuffer<RoadLaneNodeInput>(endNode).Add(new RoadLaneNodeInput { RoadLaneEnt = entity });
        entityManager.SetComponentEnabled<RoadLaneNodeUpdateInOutBuffers>(startNode, true);

        return entity;
    }

    public static Entity SpawnRoadSegmentNode(EntityManager entityManager, EntityCommandBuffer ecb, NativeArray<Entity> LaneNodes, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = entityManager.Instantiate(document.SegmentNodePrefab);
        entityManager.SetName(entity, "RoadSegmentNode_" + name);
        entityManager.GetBuffer<RoadSegmentNodeElements>(entity).Reinterpret<Entity>().AddRange(LaneNodes);
        entityManager.SetComponentEnabled<RoadSegmentNodeUpdateChildNodes>(entity, true);
        return entity;
    }
}