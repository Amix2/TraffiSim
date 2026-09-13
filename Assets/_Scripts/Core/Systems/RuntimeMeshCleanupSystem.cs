using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateInGroup(typeof(StructuralChangePresentationSystemGroup))]
public partial class RuntimeMeshCleanupSystem : SystemBase
{
    EntitiesGraphicsSystem _egs;
    EntityQuery _deadQuery;
    EntityQuery _allQuery;

    // Reused each tick so N entities sharing one mesh free it exactly once.
    readonly HashSet<BatchMeshID> _freed = new();

    protected override void OnCreate()
    {
        _egs = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();

        _deadQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<RuntimeUpdateableMesh>()
            .WithNone<RuntimeUpdateableMeshAliveTag>()
            .Build(this);

        _allQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<RuntimeUpdateableMesh>()
            .Build(this);

        RequireForUpdate(_deadQuery);
    }

    protected override void OnUpdate()
    {
        _freed.Clear();

        var entities = _deadQuery.ToEntityArray(Allocator.Temp);
        foreach (var e in entities)
        {
            var comp = EntityManager.GetComponentData<RuntimeUpdateableMesh>(e);

            // Add() returns false if this id was already freed this tick -> the
            // second of a shared pair skips both Unregister and Destroy.
            if (comp.Mesh != null && _freed.Add(comp.Id))
            {
                _egs.UnregisterMesh(comp.Id);
                Object.Destroy(comp.Mesh);
            }
            comp.Mesh = null;   // belt-and-suspenders; not load-bearing for the shared case
        }
        entities.Dispose();

        EntityManager.RemoveComponent<RuntimeUpdateableMesh>(_deadQuery);
    }

    protected override void OnDestroy()
    {
        // Teardown uses DestroyImmediate, which is synchronous: destroying the shared
        // mesh once flips its fake-null immediately, so the second comp's != null guard
        // already skips it — no set needed here.
        var entities = _allQuery.ToEntityArray(Allocator.Temp);
        foreach (var e in entities)
        {
            var comp = EntityManager.GetComponentData<RuntimeUpdateableMesh>(e);
            if (comp.Mesh != null)
            {
                Object.DestroyImmediate(comp.Mesh);
                comp.Mesh = null;
            }
        }
        entities.Dispose();
    }
}