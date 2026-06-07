using Unity.Entities;
using Unity.Mathematics;

internal static class rgSpawnHelper
{
    public static Entity SpawnRoadNode(EntityManager entityManager, EntityCommandBuffer ecb, float4x4 transform, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = ecb.Instantiate(document.NodePrefab);
        ecb.SetComponent(entity, new RoadLaneNodeData { Transform = transform });
        ecb.SetName(entity, "RoadLaneNode_" + name);

        //Entity visualizerEntity = ecb.Instantiate(document.NodeVisualizerPrefab);
        //ecb.SetComponent(visualizerEntity, new RoadLaneNodeVisualizer { RoadLaneNodeEnt = entity });
        //ecb.SetName(visualizerEntity, "RoadLaneNodeVisualizer_" + name);

        return entity;
    }

    public static Entity SpawnRoadLane(EntityManager entityManager, EntityCommandBuffer ecb, Entity startNode, Entity endNode, string name)
    {
        rgDocumentC document = rgDocumentC.GetSingletonValue(entityManager);
        Entity entity = ecb.Instantiate(document.LanePrefab);
        ecb.SetComponent(entity, new RoadLaneData { StartNodeEnt = startNode, EndNodeEnt = endNode });
        ecb.SetName(entity, "RoadLane_" + name);

        Entity visualizerEntity = ecb.Instantiate(document.LaneVisualizerPrefab);
        ecb.SetComponent(visualizerEntity, new RoadLaneVisualizer { RoadLaneEnt = entity });
        ecb.SetName(visualizerEntity, "RoadLaneVisualizer_" + name);

        return entity;
    }
}