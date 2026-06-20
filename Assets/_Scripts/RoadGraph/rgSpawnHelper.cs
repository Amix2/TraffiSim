using Unity.Entities;
using Unity.Mathematics;

internal static class rgSpawnHelper
{
    public static Entity SpawnRoadNode(EntityManager entityManager, EntityCommandBuffer ecb, float4x4 transform, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = entityManager.Instantiate(document.NodePrefab);
        entityManager.SetComponentData(entity, new RoadLaneNodeData { Transform = transform });
        entityManager.SetName(entity, "RoadLaneNode_" + name);

        //Entity visualizerEntity = ecb.Instantiate(document.NodeVisualizerPrefab);
        //ecb.SetComponent(visualizerEntity, new RoadLaneNodeVisualizer { RoadLaneNodeEnt = entity });
        //ecb.SetName(visualizerEntity, "RoadLaneNodeVisualizer_" + name);

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
            }
        }
        return entity;
    }
}