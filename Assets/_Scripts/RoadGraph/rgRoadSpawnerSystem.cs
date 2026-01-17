using Newtonsoft.Json;
using System.IO;
using Unity.Collections;
using Unity.Entities;

[UpdateBefore(typeof(rgUpdateSystem))]
[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial class rgRoadSpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<rgDocumentC>())
            return;
        var Document = SystemAPI.GetSingleton<rgDocumentC>();
        Entity nodePrefab = Document.NodePrefab;
        Entity roadManagerEnt = Document.RoadManager;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);


        foreach (var (json, jsonEnt) in SystemAPI.Query<RefRO<rgLoadRoadFromJson>>().WithEntityAccess())
        {
            string filePath = json.ValueRO.fileName.ToString();

            if (File.Exists(filePath))
            {
                string jsonFile = File.ReadAllText(filePath);
                RoadJson roadJson = JsonConvert.DeserializeObject<RoadJson>(jsonFile);
                RoadBlueprint roadBlueprint = roadJson.ToRoadBlueprint();
                foreach (var node in roadBlueprint.Nodes)
                {
                    if (node.Value.edgeCount > 0)
                    {
                        node.Value.entity = rgHelper.SpawnNode(ecb, nodePrefab, node.Value.position, roadManagerEnt);
                    }
                }
                foreach (var edge in roadBlueprint.Edges)
                {
                    Entity node1 = roadBlueprint.Nodes[edge.Node1].entity;
                    Entity node2 = roadBlueprint.Nodes[edge.Node2].entity;
                    edge.entity = rgHelper.SpawnEdge(EntityManager, ecb, node1, node2, roadManagerEnt);
                }
            }
            ecb.DestroyEntity(jsonEnt);
        }

        foreach (var (order, orderEnt) in SystemAPI.Query<RefRO<rgSpawnNodeOrder>>().WithEntityAccess())
        {
            rgHelper.SpawnNode(ecb, nodePrefab, order.ValueRO.position, roadManagerEnt);
            ecb.DestroyEntity(orderEnt);
        };

        foreach (var (order, orderEnt) in SystemAPI.Query<RefRO<rgSpawnEdgeOrder>>().WithEntityAccess())
        {
            rgHelper.SpawnEdge(EntityManager, ecb, order.ValueRO.Node0, order.ValueRO.Node1, roadManagerEnt);
            ecb.DestroyEntity(orderEnt);
        }
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}