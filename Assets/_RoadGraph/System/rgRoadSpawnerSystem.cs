using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class rgRoadSpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<RoadJsonStringByte>();
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        var toDestroy = new NativeList<Entity>(Allocator.Temp);

        EntityCommandBuffer ecb = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        foreach (var (request, entity) in
                 SystemAPI.Query<DynamicBuffer<RoadJsonStringByte>>().WithEntityAccess())
        {
            string jsonStr = StringBufferUtility.GetString<RoadJsonStringByte>(request);
            RoadBlueprint roadBlueprint = JsonSerializable.FromJsonString<RoadBlueprint>(jsonStr);

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

            foreach (RoadPort roadPort in roadBlueprint.RoadPorts)
            {
                Entity entity = IdToEntity[roadPort.Id];
                RoadPortAspect roadPortAspect = new(this, entity);

                foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
                {
                    if (roadLane.StartPort == roadPort.Id)
                        roadPortAspect.AddOutputLane(IdToEntity[roadLane.StartPort]);
                    if (roadLane.EndPort == roadPort.Id)
                        roadPortAspect.AddInputLane(IdToEntity[roadLane.EndPort]);
                }
                roadPortAspect.Position = roadPort.PositionFl3;
            }

            foreach (RoadLane roadLane in roadBlueprint.RoadLanes)
            {
                Entity entity = IdToEntity[roadLane.Id];
                RoadLaneAspect roadLaneAspect = new(this, entity);
                roadLaneAspect.SetStartPort(IdToEntity[roadLane.StartPort]);
                roadLaneAspect.SetEndPort(IdToEntity[roadLane.EndPort]);
                roadLaneAspect.SetLaneWidth(roadLane.Width);
            }

            foreach (RoadNode roadNode in roadBlueprint.RoadNodes)
            {
                Entity entity = IdToEntity[roadNode.Id];
                RoadNodeAspect roadNodeAspect = new(this, entity);
                NativeList<float2> portPositions = new(Allocator.Temp);

                foreach (Guid roadPort in roadNode.Ports)
                {
                    roadNodeAspect.AddChild(IdToEntity[roadPort]);
                    portPositions.Add(EntityToPort[IdToEntity[roadPort]].PositionFl3.xz);
                    RoadPortAspect roadPortAspect = new(this, IdToEntity[roadPort]);
                    roadPortAspect.Parent = entity;
                }
                roadNodeAspect.RecalculateLane(portPositions.AsArray());
                roadNodeAspect.SortChildren(GetComponentLookup<RoadPortData>(true));
            }

            foreach (RoadSegment roadSegment in roadBlueprint.RoadSegments)
            {
                Entity entity = IdToEntity[roadSegment.Id];
                RoadSegmentAspect roadSegmentAspect = new(this, entity);

                foreach (Guid node in roadSegment.Nodes)
                    roadSegmentAspect.AddNode(IdToEntity[node]);

                foreach (Guid roadLane in roadSegment.Lanes)
                {
                    roadSegmentAspect.AddChildLane(IdToEntity[roadLane]);
                    RoadLaneAspect roadLaneAspect = new(this, IdToEntity[roadLane]);
                    roadLaneAspect.SetParent(entity);
                }
            }
        }
    }
}