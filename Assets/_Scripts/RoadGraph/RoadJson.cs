using System.Collections.Generic;
using Unity.Mathematics;

[System.Serializable]
public class RoadJson
{
    /*
    {
        "Nodes":
        {
            "0": [-10,-10],
            "1": [10,-10],
            "2": [10,10],
            "3": [-10,10]
        },
        "Edges":
        [
            [0,1],
            [1,2],
            [2,3],
            [3,0]
        ]
    }
     */
    public Dictionary<int, List<float>> Nodes = new();  // ID -> list of 2 or 3 coordinates
    public List<List<int>> Edges = new();   // list of pairs of node IDs

    public RoadBlueprint ToRoadBlueprint()
    {
        RoadBlueprint blueprint = new RoadBlueprint();
        foreach (var node in Nodes)
            blueprint.AddNode(node.Key, new RoadBlueprint.Node(node.Value));

        foreach (var edge in Edges)
            blueprint.AddEdge(new RoadBlueprint.Edge(edge));
        return blueprint;
    }

    public void AddNode(int id, float3 position)
    {
        Nodes.Add(id, new List<float>() { position.x, position.y, position.z });
    }

    public void AddEdge(int node1, int node2)
    {
        Unity.Assertions.Assert.IsTrue(Nodes.ContainsKey(node1));
        Unity.Assertions.Assert.IsTrue(Nodes.ContainsKey(node2));
        Edges.Add(new List<int> { node1, node2 });
    }
}