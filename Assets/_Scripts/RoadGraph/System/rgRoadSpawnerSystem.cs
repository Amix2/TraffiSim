using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial class rgRoadSpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<rgSpawnRoadDataFromJsonText>();
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        var toDestroy = new NativeList<Entity>(Allocator.Temp);

        EntityCommandBuffer ecb = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        foreach (var (request, entity) in
                 SystemAPI.Query<rgSpawnRoadDataFromJsonText>().WithEntityAccess())
        {
            RoadBlueprint roadBlueprint = JsonSerializable.FromJsonString<RoadBlueprint>(request.JsonText);

            SpawnRoadFromBlueprint(ecb, roadBlueprint);
            toDestroy.Add(entity);
        }

        EntityManager.DestroyEntity(toDestroy.AsArray());
        toDestroy.Dispose();
    }

    private void SpawnRoadFromBlueprint(EntityCommandBuffer ecb, RoadBlueprint roadBlueprint)
    {
        Dictionary<Guid, Entity> IdToEntity = new();
        Dictionary<Entity, RoadLaneNode> EntityToNode = new();

        foreach (RoadLaneNode roadNode in roadBlueprint.RoadLaneNodes)
        {
            Entity nodeEntity = rgSpawnHelper.SpawnRoadLaneNode(EntityManager, ecb, new float4x4(float3x3.RotateZ(math.radians(90f)), roadNode.PositionFl3), roadNode.Id.ToString().Split('-')[^1]);
            Debug.Assert(IdToEntity.ContainsKey(roadNode.Id) == false);
            IdToEntity[roadNode.Id] = nodeEntity;
            EntityToNode[nodeEntity] = roadNode;
        }

        foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
        {
            Entity startNodeEntity = IdToEntity[roadLane.StartNode];
            Entity endNodeEntity = IdToEntity[roadLane.EndNode];
            Entity laneEntity = rgSpawnHelper.SpawnRoadLane(EntityManager, ecb, startNodeEntity, endNodeEntity, $"{roadLane.StartNode.ToString().Split('-')[^1]}->{roadLane.EndNode.ToString().Split('-')[^1]}");
            Debug.Assert(IdToEntity.ContainsKey(roadLane.Id) == false);
            IdToEntity[roadLane.Id] = laneEntity;
        }
        foreach (RoadSegmentNode roadSegmentNode in roadBlueprint.RoadSegmentNodes)
        {
            NativeArray<Entity> nodes = new NativeArray<Entity>(roadSegmentNode.LaneNodes.Count, Allocator.Temp);
            for(int i=0; i<nodes.Length; i++)
            {
                Guid nodeGuid = roadSegmentNode.LaneNodes[i];
                Debug.Assert(IdToEntity.ContainsKey(nodeGuid));
                nodes[i] = IdToEntity[nodeGuid];
            }
            Entity nodeEntity = rgSpawnHelper.SpawnRoadSegmentNode(EntityManager, ecb, nodes, roadSegmentNode.Id.ToString().Split('-')[^1]);
            Debug.Assert(IdToEntity.ContainsKey(roadSegmentNode.Id) == false);
            IdToEntity[roadSegmentNode.Id] = nodeEntity;
        }
    }
}