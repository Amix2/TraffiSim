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

    private string GuidToName(Guid guid)
    {
        return guid.ToString().Split('-')[^1];
    }

    private void SpawnRoadFromBlueprint(EntityCommandBuffer ecb, RoadBlueprint roadBlueprint)
    {
        Dictionary<Guid, Entity> IdToEntity = new();
        Dictionary<Entity, RoadNode> EntityToNode = new();
        Dictionary<Entity, RoadSegment> EntityToSegment = new();
        Dictionary<Entity, RoadPort> EntityToPort = new();
        Dictionary<Entity, RoadLane> EntityToLanes = new();

        {   // create entities from prefabs
            foreach (RoadNode roadNode in roadBlueprint.RoadNodes)
            {
                Entity nodeEntity = rgSpawnHelper.SpawnRoadPrefab(EntityManager, ecb, rgDocumentC.Prefab.Node, GuidToName(roadNode.Id));
                Debug.Assert(IdToEntity.ContainsKey(roadNode.Id) == false);
                IdToEntity[roadNode.Id] = nodeEntity;
                EntityToNode[nodeEntity] = roadNode;
            }

            foreach (RoadSegment roadSegment in roadBlueprint.RoadSegments)
            {
                Entity entity = rgSpawnHelper.SpawnRoadPrefab(EntityManager, ecb, rgDocumentC.Prefab.Segment, GuidToName(roadSegment.Id));
                Debug.Assert(IdToEntity.ContainsKey(roadSegment.Id) == false);
                IdToEntity[roadSegment.Id] = entity;
                EntityToSegment[entity] = roadSegment;
            }

            foreach (RoadPort roadPort in roadBlueprint.RoadPorts)
            {
                Entity entity = rgSpawnHelper.SpawnRoadPrefab(EntityManager, ecb, rgDocumentC.Prefab.Port, GuidToName(roadPort.Id));
                Debug.Assert(IdToEntity.ContainsKey(roadPort.Id) == false);
                IdToEntity[roadPort.Id] = entity;
                EntityToPort[entity] = roadPort;
            }

            foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
            {
                Entity entity = rgSpawnHelper.SpawnRoadPrefab(EntityManager, ecb, rgDocumentC.Prefab.Lane, GuidToName(roadLane.Id));
                Debug.Assert(IdToEntity.ContainsKey(roadLane.Id) == false);
                IdToEntity[roadLane.Id] = entity;
                EntityToLanes[entity] = roadLane;
            }
        }

        {   // setup
            foreach (RoadSegment roadSegment in roadBlueprint.RoadSegments)
            {
                Entity entity = IdToEntity[roadSegment.Id];

                NativeList<Entity> Nodes = new(Allocator.Temp);
                foreach (Guid node in roadSegment.Nodes)
                    Nodes.Add(IdToEntity[node]);

                NativeList<Entity> Lanes = new(Allocator.Temp);
                foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
                    if (roadLane.Parent == roadSegment.Id)
                        Lanes.Add(IdToEntity[roadLane.Id]);

                rgSpawnHelper.SetupRoadSegment(EntityManager, ecb, entity, Nodes.AsArray(), Lanes.AsArray());
            }

            foreach (RoadPort roadPort in roadBlueprint.RoadPorts)
            {
                Entity entity = IdToEntity[roadPort.Id];


                float4x4 transform = new float4x4(float3x3.RotateZ(math.radians(90f)), roadPort.PositionFl3);
                NativeList<Entity> inputs = new(Allocator.Temp);
                NativeList<Entity> outputs = new(Allocator.Temp);
                foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
                {
                    if(roadLane.StartPort == roadPort.Id)
                        outputs.Add(IdToEntity[roadLane.StartPort]);
                    if(roadLane.EndPort == roadPort.Id)
                        inputs.Add(IdToEntity[roadLane.EndPort]);
                }
                rgSpawnHelper.SetupRoadPortData data = new()
                {
                    transform = transform,
                    Parent = IdToEntity[roadPort.Parent],
                    InputLanes = inputs.AsArray(),
                    OutputLanes = outputs.AsArray()
                };
                rgSpawnHelper.SetupRoadPort(EntityManager, ecb, entity, data);
            }
        }
    }
}