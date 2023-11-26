using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static RoadBlueprint;

[UpdateBefore(typeof(rgUpdateSystem))]
[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial class rgRoadSpawnerSystem : SystemBase
{
    private Entity SpawnNode(EntityManager manager, Entity prefab, float3 position, Entity RoadManagerEnt)
    {
        var node = manager.Instantiate(prefab);
        manager.SetComponentData(node, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = 1 });
        manager.AddBuffer<rgNodeEdges>(node);

        manager.GetBuffer<rgRoadNodes>(RoadManagerEnt).Add(new rgRoadNodes { Node = node });
        return node;
    }

    public Entity SpawnEdge(EntityManager manager, Entity Node1, Entity Node2, Entity RoadManagerEnt)
    {
        NativeList<ComponentType> types = new(4, Allocator.Temp)
        {
            ComponentType.ReadOnly<rgEdge>(),
            ComponentType.ReadOnly<rgEdgePosiotions>(),
            ComponentType.ReadOnly<SceneSection>(),
            ComponentType.ReadOnly<SceneTag>()
        };

        var arch = manager.CreateArchetype(types.AsArray());
        var edge = manager.CreateEntity(arch);
        manager.SetComponentData(edge, new rgEdge { Node1 = Node1, Node2 = Node2 });
        manager.SetName(edge, "rgEdge");
        SceneSection sceneSection = manager.GetSharedComponent<SceneSection>(RoadManagerEnt);
        SceneTag sceneTag = manager.GetSharedComponent<SceneTag>(RoadManagerEnt);
        manager.SetSharedComponent(edge, sceneSection);
        manager.SetSharedComponent(edge, sceneTag);

        manager.GetBuffer<rgRoadEdges>(RoadManagerEnt).Add(new rgRoadEdges { Edge = edge });
        return edge;
    }

    [System.Serializable]
    private class RoadJson
    {
        public Dictionary<int, List<float>> Nodes;
        public List<List<int>> Edges;

        public RoadBlueprint ToRoadBlueprint()
        {
            RoadBlueprint blueprint = new RoadBlueprint();
            foreach (var node in Nodes)
                blueprint.AddNode(node.Key, new Node(node.Value));

            foreach (var edge in Edges)
                blueprint.AddEdge(new Edge(edge));
            return blueprint;
        }
    }

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
    }
}