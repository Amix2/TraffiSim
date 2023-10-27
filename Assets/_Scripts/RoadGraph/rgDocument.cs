using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class rgDocument : MonoBehaviour
{
    public GameObject NodePrefabGO;

    public partial class Baker : Baker<rgDocument>
    {
        public override void Bake(rgDocument authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            Entity RoadManager = CreateAdditionalEntity(TransformUsageFlags.None, false, "RoadManager");

            AddComponent(entity, new rgDocumentC
            {
                NodePrefab = GetEntity(authoring.NodePrefabGO, TransformUsageFlags.Renderable),
                RoadManager = RoadManager
            });

            AddBuffer<rgRoadNodes>(RoadManager);
            AddBuffer<rgRoadEdges>(RoadManager);
        }
    }
}

public struct rgDocumentC : IComponentData
{
    public Entity NodePrefab;
    public Entity RoadManager;
}

public readonly partial struct rgDocumentAspect : IAspect
{
    public readonly Entity DocumentEntity;

    private readonly RefRW<rgDocumentC> DocumentComponent;

    public Entity NodePrefab => DocumentComponent.ValueRO.NodePrefab;
    public Entity RoadManagerEnt => DocumentComponent.ValueRW.RoadManager;

    public Entity SpawnNode(ref EntityManager manager, float3 position)
    {
        var node = manager.Instantiate(NodePrefab);
        manager.SetComponentData(node, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = 1 });

        manager.GetBuffer<rgRoadNodes>(RoadManagerEnt).Add(new rgRoadNodes { Node = node });

        return node;
    }

    public Entity SpawnEdge(ref EntityManager manager, Entity Node1, Entity Node2)
    {
        var edge = manager.CreateEntity(typeof(rgEdge), typeof(rgEdgePosiotions));
        manager.SetComponentData(edge, new rgEdge { Node1 = Node1, Node2 = Node2 });
        manager.SetName(edge, "rgEdge");
        manager.GetBuffer<rgRoadEdges>(RoadManagerEnt).Add(new rgRoadEdges { Edge = edge });
        return edge;

        // causes structural changes and document breaks
        //SceneSection sceneSection = manager.GetSharedComponentManaged<SceneSection>(DocumentEntity);
        //SceneTag sceneTag = manager.GetSharedComponentManaged<SceneTag>(DocumentEntity);
        //manager.SetSharedComponentManaged(edge, sceneSection);
        //manager.SetSharedComponentManaged(edge, sceneTag);
    }
}