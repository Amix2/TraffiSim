using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class rgHelper
{
    public static Entity SpawnNode(EntityCommandBuffer ecb, Entity prefab, float3 position, Entity RoadManagerEnt)
    {
        var node = ecb.Instantiate(prefab);
        ecb.SetComponent(node, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = 1 });
        ecb.AddBuffer<rgOutgoingNodeEdges>(node);
        ecb.AddBuffer<rgIncomingNodeEdges>(node);

        ecb.AppendToBuffer(RoadManagerEnt, new rgRoadNodes { Node = node });
        return node;
    }

    public static Entity SpawnEdge(EntityManager manager, EntityCommandBuffer ecb, Entity StartNode, Entity EndNode, Entity RoadManagerEnt)
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
        var edge = ecb.CreateEntity(arch);
        ecb.SetComponent(edge, new rgEdge { Start = StartNode, End = EndNode });
        ecb.SetName(edge, "rgEdge");
        SceneSection sceneSection = manager.GetSharedComponent<SceneSection>(RoadManagerEnt);
        SceneTag sceneTag = manager.GetSharedComponent<SceneTag>(RoadManagerEnt);
        ecb.SetSharedComponent(edge, sceneSection);
        ecb.SetSharedComponent(edge, sceneTag);

        ecb.AppendToBuffer(RoadManagerEnt, new rgRoadEdges { Edge = edge });
        return edge;
    }
}