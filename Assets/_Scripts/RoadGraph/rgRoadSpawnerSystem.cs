using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
    }

    private class RoadBlueprint
    {
        public class Node
        {
            public readonly float3 position;
            public int edgeCount;
            public Entity entity;

            public Node(List<float> args)
            {
                edgeCount = 0;
                entity = Entity.Null;
                position = args.Count == 2 ? new float3(args[0], 0, args[1]) : new float3(args[0], args[1], args[2]);
            }
        }

        public class Edge
        {
            public readonly int Node1, Node2;
            public Entity entity;

            public Edge(List<int> args)
            {
                Assert.IsTrue(args.Count == 2);
                Node1 = args[0];
                Node2 = args[1];
            }
        }

        public readonly Dictionary<int, Node> Nodes = new Dictionary<int, Node>();
        public readonly List<Edge> Edges = new List<Edge>();

        public RoadBlueprint(RoadJson roadJson)
        {
            foreach (var node in roadJson.Nodes)
                Nodes.Add(node.Key, new Node(node.Value));

            foreach (var edge in roadJson.Edges)
            {
                Edges.Add(new Edge(edge));
                Nodes[edge[0]].edgeCount++;
                Nodes[edge[1]].edgeCount++;
            }
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
                RoadBlueprint roadBlueprint = new RoadBlueprint(roadJson);
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