using Unity.Burst;
using Unity.Collections;   // for [ReadOnly]
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Example
{
    // =============================================================================
    //  ISystem + IJobEntity reference example (Unity Entities 1.x)
    //
    //  Scenario: each matching entity drifts by its velocity, is pulled toward the
    //  position of a SEPARATE "target" entity (read via ComponentLookup), and counts
    //  down a lifetime. When the lifetime hits zero the entity is destroyed.
    //
    //  Demonstrates:
    //    - an unmanaged ISystem with Burst-compiled OnCreate/OnUpdate/OnDestroy
    //    - RequireForUpdate as a cheap early-out
    //    - an IJobEntity whose query is INFERRED from its Execute parameters
    //    - ref vs in parameters, the Entity param, and [EntityIndexInQuery]
    //    - a [WithAll] query-refinement attribute (a tag that isn't an Execute arg)
    //    - ComponentLookup<T> for random-access reads of OTHER entities
    //    - structural changes from inside a job via EntityCommandBuffer.ParallelWriter
    //    - parallel scheduling with correct state.Dependency chaining
    // =============================================================================

    // -----------------------------------------------------------------------------
    //  COMPONENTS
    // -----------------------------------------------------------------------------

    public struct Velocity : IComponentData
    {
        public float3 Value;
    }

    public struct Lifetime : IComponentData
    {
        public float Remaining;
    }

    // A zero-size tag. Used to opt entities into this system via [WithAll].
    public struct Moving : IComponentData
    { }

    // Each moving entity points at another entity it should home toward.
    public struct Target : IComponentData
    {
        public Entity Value;
    }

    // Lives on the TARGET entities. The moving entity reads this via ComponentLookup.
    // NOTE: this is a DISTINCT type from anything written via 'ref' in the query
    // (LocalTransform, Lifetime). That's deliberate — see the aliasing note below.
    public struct Attractor : IComponentData
    {
        public float3 Position;
        public float Strength;
    }

    // -----------------------------------------------------------------------------
    //  THE JOB
    // -----------------------------------------------------------------------------

    [BurstCompile]
    [WithAll(typeof(Moving))]
    public partial struct MoveAndExpireJob : IJobEntity
    {
        // Inputs set on the main thread, copied by value into every worker.
        public float DeltaTime;

        // Parallel writer for safe structural changes (recorded now, played back later).
        public EntityCommandBuffer.ParallelWriter ECB;

        // RANDOM-ACCESS LOOKUP. Lets the job read an Attractor on ANY entity by its
        // Entity handle — not just the entity currently being iterated. Mark it
        // [ReadOnly] when you only read; this both documents intent and relaxes the
        // job safety system so it can run in parallel.
        //
        // ALIASING RULE: if you make a ComponentLookup<T> of a type T that is ALSO
        // written via 'ref' in this same query, ScheduleParallel will throw — a
        // looked-up entity could live in a chunk being written on another thread
        // (a data race). We avoid that here by looking up Attractor, which is never
        // written in the query. (If you must alias, use
        // [NativeDisableParallelForRestriction] and guarantee no overlap yourself.)
        [ReadOnly] public ComponentLookup<Attractor> AttractorLookup;

        public void Execute(
            Entity entity,
            [EntityIndexInQuery] int sortKey,
            ref LocalTransform transform,
            in Velocity velocity,
            ref Lifetime lifetime,
            in Target target)
        {
            // 1) Base drift.
            transform.Position += velocity.Value * DeltaTime;

            // 2) Homing via ComponentLookup. TryGetComponent safely handles the case
            //    where the target entity was destroyed or never had an Attractor —
            //    no need for a separate HasComponent check.
            if (AttractorLookup.TryGetComponent(target.Value, out Attractor attractor))
            {
                float3 toTarget = attractor.Position - transform.Position;
                transform.Position += toTarget * (attractor.Strength * DeltaTime);
            }

            // 3) Lifetime countdown + queued destroy on expiry.
            lifetime.Remaining -= DeltaTime;
            if (lifetime.Remaining <= 0f)
            {
                ECB.DestroyEntity(sortKey, entity);
            }
        }
    }

    // -----------------------------------------------------------------------------
    //  THE SYSTEM
    // -----------------------------------------------------------------------------

    [BurstCompile]
    public partial struct MoveAndExpireSystem : ISystem
    {
        // Cache the lookup handle once. Re-creating it every frame is wasteful;
        // caching + calling .Update() each frame is the recommended pattern.
        private ComponentLookup<Attractor> _attractorLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Lifetime>();

            // Acquire the lookup once, read-only.
            _attractorLookup = state.GetComponentLookup<Attractor>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // MANDATORY each frame: refresh the lookup's internal safety/version
            // handles for the current frame. Forgetting .Update(ref state) is the
            // single most common ComponentLookup bug (stale-handle exceptions).
            _attractorLookup.Update(ref state);

            var ecbSingleton =
                SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            EntityCommandBuffer.ParallelWriter ecb =
                ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new MoveAndExpireJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ECB = ecb,
                AttractorLookup = _attractorLookup,
            };

            // Pass state.Dependency IN, assign the returned handle back OUT.
            state.Dependency = job.ScheduleParallel(state.Dependency);

            // ---- Scheduling variants (pick ONE) ----
            //   job.Run();                                       // main thread, immediate
            //   state.Dependency = job.Schedule(state.Dependency);       // single worker
            //   state.Dependency = job.ScheduleParallel(state.Dependency); // many workers
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }
    }
}