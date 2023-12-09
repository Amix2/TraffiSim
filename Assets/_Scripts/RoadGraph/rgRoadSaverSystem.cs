using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(rgUpdateSystem))]
[UpdateInGroup(typeof(RoadGraphSystemGroup))]
public partial class rgRoadSaverSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentC>());

        Entities.WithStructuralChanges().ForEach((Entity jsonEnt, in rgSaveRoadFromJson json) =>
        {
            rgRoadManagerAspect roadManager = EntityManager.GetAspect<rgRoadManagerAspect>(Document.RoadManagerEnt);
            Dictionary<Entity, int> nodeIds = new Dictionary<Entity, int>();

            RoadJson roadJson = new RoadJson();
            int nodeId = 0;
            foreach (var node in roadManager.GetNodes)
            {
                nodeIds.Add(node, nodeId++);
                float3 nodePos = EntityManager.GetComponentData<LocalTransform>(node).Position;
                roadJson.AddNode(nodeIds[node], nodePos);
            }

            foreach (var edgeEnt in roadManager.GetEdges)
            {
                rgEdge edge = EntityManager.GetComponentData<rgEdge>(edgeEnt);
                roadJson.AddEdge(nodeIds[edge.Node1], nodeIds[edge.Node2]);
            }
            string jsonText = JsonConvert.SerializeObject(roadJson);

            File.WriteAllText(json.fileName.ToString(), jsonText);
            EntityManager.DestroyEntity(jsonEnt);
        }).Run();
    }
}