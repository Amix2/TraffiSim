using System.Collections.Generic;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;

public class RoadBlueprint
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

    public void AddNode(int key, Node node)
    {
        Nodes.Add(key, node);
    }
    public void AddEdge(Edge edge)
    {
        Edges.Add(edge);
        Nodes[edge.Node1].edgeCount++;
        Nodes[edge.Node2].edgeCount++;
    }
}
