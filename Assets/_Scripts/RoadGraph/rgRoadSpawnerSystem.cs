using Newtonsoft.Json;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(rgUpdateSystem))]
[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial class rgRoadSpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentC>());
        Entity nodePrefab = Document.NodePrefab;
        Entity roadManagerEnt = Document.RoadManagerEnt;

        Entities.WithStructuralChanges().ForEach((Entity jsonEnt, in rgLoadRoadFromJson json) =>
        {
            string filePath = json.fileName.ToString();

            if (File.Exists(filePath))
            {
                string jsonFile = File.ReadAllText(filePath);
                RoadJson roadJson = JsonConvert.DeserializeObject<RoadJson>(jsonFile);
                RoadBlueprint roadBlueprint = roadJson.ToRoadBlueprint();
                foreach (var node in roadBlueprint.Nodes)
                {
                    if (node.Value.edgeCount > 0)
                    {
                        node.Value.entity = rgHelper.SpawnNode(EntityManager, nodePrefab, node.Value.position, roadManagerEnt);
                    }
                }
                foreach (var edge in roadBlueprint.Edges)
                {
                    Entity node1 = roadBlueprint.Nodes[edge.Node1].entity;
                    Entity node2 = roadBlueprint.Nodes[edge.Node2].entity;
                    edge.entity = rgHelper.SpawnEdge(EntityManager, node1, node2, roadManagerEnt);
                }
            }
            EntityManager.DestroyEntity(jsonEnt);
        }).Run();

        Entities.WithStructuralChanges().ForEach((Entity orderEnt, in rgSpawnNodeOrder order) =>
        {
            rgHelper.SpawnNode(EntityManager, nodePrefab, order.position, roadManagerEnt);
            EntityManager.DestroyEntity(orderEnt);
        }).Run();

        Entities.WithStructuralChanges().ForEach((Entity orderEnt, in rgSpawnEdgeOrder order) =>
        {
            rgHelper.SpawnEdge(EntityManager, order.Node0, order.Node1, roadManagerEnt);
            EntityManager.DestroyEntity(orderEnt);
        }).Run();
    }
}