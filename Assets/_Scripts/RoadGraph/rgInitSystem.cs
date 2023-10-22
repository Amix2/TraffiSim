using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct rgInitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var Document = SystemAPI.GetAspect<rgDocumentAspect>(SystemAPI.GetSingletonEntity<rgDocumentAspect>());
        var nodePrefab = Document.NodePrefab;
        var manager = state.EntityManager;


        var Node1 = Document.SpawnNode(manager, new float3(-10, 0, -10));
        var Node2 = Document.SpawnNode(manager, new float3(10, 0, 10));

        Document.SpawnEdge(manager, Node1, Node2);  
    }

    [BurstCompile]
    void OnCreate(ref SystemState state) { }

    [BurstCompile]
    void OnDestroy(ref SystemState state) { }
}
