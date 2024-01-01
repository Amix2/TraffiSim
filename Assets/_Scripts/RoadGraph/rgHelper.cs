using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class rgHelper
{
    public static Entity SpawnNode(EntityManager manager, Entity prefab, float3 position, Entity RoadManagerEnt)
    {
        var node = manager.Instantiate(prefab);
        manager.SetComponentData(node, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = 1 });
        manager.AddBuffer<rgOutgoingNodeEdges>(node);
        manager.AddBuffer<rgIncomingNodeEdges>(node);

        manager.GetBuffer<rgRoadNodes>(RoadManagerEnt).Add(new rgRoadNodes { Node = node });
        return node;
    }

    public static Entity SpawnEdge(EntityManager manager, Entity StartNode, Entity EndNode, Entity RoadManagerEnt)
    {
        NativeList<ComponentType> types = new(4, Allocator.Temp)
        {
            ComponentType.ReadOnly<rgEdge>(),
            ComponentType.ReadOnly<rgEdgePosiotions>(),
            ComponentType.ReadOnly<rgEdgeOccupant>(),
            ComponentType.ReadOnly<SceneSection>(),
            ComponentType.ReadOnly<SceneTag>(),
        };

        var arch = manager.CreateArchetype(types.AsArray());
        var edge = manager.CreateEntity(arch);
        manager.SetComponentData(edge, new rgEdge { Start = StartNode, End = EndNode });
        manager.SetName(edge, "rgEdge");
        SceneSection sceneSection = manager.GetSharedComponent<SceneSection>(RoadManagerEnt);
        SceneTag sceneTag = manager.GetSharedComponent<SceneTag>(RoadManagerEnt);
        manager.SetSharedComponent(edge, sceneSection);
        manager.SetSharedComponent(edge, sceneTag);

        manager.GetBuffer<rgRoadEdges>(RoadManagerEnt).Add(new rgRoadEdges { Edge = edge });
        return edge;
    }
}