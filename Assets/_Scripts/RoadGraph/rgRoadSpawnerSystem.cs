using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
        Dictionary<Entity, RoadNode> EntityToNode = new();

        foreach (RoadNode roadNode in roadBlueprint.RoadNodes)
        {
            Entity nodeEntity = rgSpawnHelper.SpawnRoadNode(EntityManager, ecb, new float4x4(float3x3.RotateZ(math.radians(90f)), roadNode.PositionFl3), roadNode.Id.ToString().Split('-')[^1]);
            IdToEntity[roadNode.Id] = nodeEntity;
            EntityToNode[nodeEntity] = roadNode;
        }

        foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
        {
            Entity startNodeEntity = IdToEntity[roadLane.StartNode];
            Entity endNodeEntity = IdToEntity[roadLane.EndNode];
            rgSpawnHelper.SpawnRoadLane(EntityManager, ecb, startNodeEntity, endNodeEntity, $"{roadLane.StartNode.ToString().Split('-')[^1]}->{roadLane.EndNode.ToString().Split('-')[^1]}");
        }
    }
}