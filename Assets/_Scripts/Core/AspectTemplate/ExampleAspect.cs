using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

// ===========================================================================
// EXAMPLE COMPONENTS — one per access style, plus a buffer and a query tag.
// ===========================================================================

/// Read-only in the aspect -> RefRO field.
public struct ExampleSpeed : IComponentData
{
    public float Value;
}

/// Read-write in the aspect -> RefRW field.
public struct ExamplePosition : IComponentData
{
    public float3 Value;
}

/// Small, read + replaced whole -> RefRW-backed Value property.
public struct ExampleLevel : IComponentData
{
    public int Value;
}

/// Optional component — not on every entity -> Optional bind + Has check.
public struct ExampleBoost : IComponentData
{
    public float Multiplier;
}

/// Dynamic buffer -> DynamicBuffer field.
public struct ExampleWaypoint : IBufferElementData
{
    public float3 Position;
}

/// Tag used to constrain job queries.
public struct ExampleTag : IComponentData
{
}

// ===========================================================================
// EXAMPLE ASPECT — eager binding, branch-free access.
//
// Lookup lifecycle: Initialize(ref state) constructs every slot read-only
// (always-valid job data, full safety checks, no exemption attributes),
// then Request(...) only the slots the system uses. The indexer eagerly
// binds requested slots; unrequested ones bind as invalid refs and are
// never fetched through. Systems can also skip the Lookup entirely and
// build aspects from Execute parameters / standalone slots — see
// ExamplePrimedSystem and ExampleMixedSystem.
//
// An aspect can also be built WITHOUT any Lookup, straight from IJobEntity
// Execute parameters, via the object initializer — see ExamplePrimedSystem.
// ===========================================================================
public struct ExampleAspect
{
    public struct Lookup
    {
        public LookupSlot<ExampleSpeed> Speed;
        public LookupSlot<ExamplePosition> Position;
        public LookupSlot<ExampleLevel> Level;
        public LookupSlot<ExampleBoost> Boost;
        public BufferSlot<ExampleWaypoint> Waypoints;

        /// Constructs every slot read-only so the Lookup is always valid
        /// job data. Call once in OnCreate, BEFORE the Requests.
        public void Initialize(ref SystemState state)
        {
            Speed.Initialize(ref state);
            Position.Initialize(ref state);
            Level.Initialize(ref state);
            Boost.Initialize(ref state);
            Waypoints.Initialize(ref state);
        }

        /// SystemBase / managed system variant.
        public void Initialize(ComponentSystemBase system)
        {
            Speed.Initialize(system);
            Position.Initialize(system);
            Level.Initialize(system);
            Boost.Initialize(system);
            Waypoints.Initialize(system);
        }

        public void Update(ref SystemState state)
        {
            Speed.Update(ref state);
            Position.Update(ref state);
            Level.Update(ref state);
            Boost.Update(ref state);
            Waypoints.Update(ref state);
        }

        /// SystemBase / managed system variant.
        public void Update(SystemBase system)
        {
            Speed.Update(system);
            Position.Update(system);
            Level.Update(system);
            Boost.Update(system);
            Waypoints.Update(system);
        }

        // Eagerly binds every REQUESTED component; unrequested slots bind
        // as invalid refs. One line per component.
        public ExampleAspect this[Entity e] => new ExampleAspect
        {
            Entity = e,
            Speed = Speed.BindRO(e),
            Position = Position.BindRW(e),
            LevelRW = Level.BindRW(e),
            Boost = Boost.BindROOptional(e),   // optional: invalid if absent
            Waypoints = Waypoints.Bind(e),
        };
    }

    public Entity Entity;

    // Plain ref fields — no wrapper, no validity branch, directly assignable
    // when constructing an aspect manually from Execute parameters.
    public RefRO<ExampleSpeed> Speed;
    public RefRW<ExamplePosition> Position;
    public RefRW<ExampleLevel> LevelRW;
    public RefRO<ExampleBoost> Boost;
    public DynamicBuffer<ExampleWaypoint> Waypoints;

    /// Value-style convenience over LevelRW.
    public ExampleLevel Level
    {
        get => LevelRW.ValueRO;
        set => LevelRW.ValueRW = value;
    }

    /// Optional component gate — an unbound/absent ref is invalid.
    public bool HasBoost => Boost.IsValid;

    // -----------------------------------------------------------------------
    // Multi-component methods
    // -----------------------------------------------------------------------

    /// Moves toward the first waypoint; pops it when reached.
    /// Uses: Position (RW), Speed (RO), Boost (optional RO), Waypoints (buffer).
    public void MoveAlongWaypoints(float deltaTime)
    {
        if (Waypoints.Length == 0)
            return;

        float speed = Speed.ValueRO.Value;
        if (HasBoost)
            speed *= Boost.ValueRO.Multiplier;

        float3 target = Waypoints[0].Position;
        float3 current = Position.ValueRO.Value;
        float3 toTarget = target - current;
        float distance = math.length(toTarget);
        float step = speed * deltaTime;

        if (step >= distance)
        {
            Position.ValueRW.Value = target;
            Waypoints.RemoveAt(0);
        }
        else
        {
            Position.ValueRW.Value = current + toTarget * (step / distance);
        }
    }

    /// Uses: Level (Value get + set), Waypoints (buffer write).
    public void LevelUpAndAddPatrolPoint(float3 point)
    {
        Level = new ExampleLevel { Value = Level.Value + 1 };
        Waypoints.Add(new ExampleWaypoint { Position = point });
    }
}

// ===========================================================================
// ISystem — Style 1: Entity-only Execute, aspect bound from the Lookup.
// ===========================================================================
[BurstCompile]
public partial struct ExampleSystem : ISystem
{
    ExampleAspect.Lookup _lookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _lookup.Initialize(ref state);
        _lookup.Speed.Request(ref state, isReadOnly: true);
        _lookup.Position.Request(ref state, isReadOnly: false);
        _lookup.Level.Request(ref state, isReadOnly: false);
        _lookup.Boost.Request(ref state, isReadOnly: true);
        _lookup.Waypoints.Request(ref state, isReadOnly: false);

        state.RequireForUpdate<ExampleTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _lookup.Update(ref state);   // never skip this

        new ExampleJob
        {
            Lookup = _lookup,
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule();
    }

    // Query constrained by attributes; Boost is optional so it is NOT listed.
    [BurstCompile]
    [WithAll(typeof(ExampleTag), typeof(ExampleSpeed), typeof(ExamplePosition),
             typeof(ExampleLevel), typeof(ExampleWaypoint))]
    partial struct ExampleJob : IJobEntity
    {
        public ExampleAspect.Lookup Lookup;
        public float DeltaTime;

        void Execute(Entity entity)
        {
            var aspect = Lookup[entity];   // all components fetched HERE
            aspect.MoveAlongWaypoints(DeltaTime);

            if (aspect.Waypoints.Length == 0)
                aspect.LevelUpAndAddPatrolPoint(new float3(0, 0, 0));
        }
    }
}

// ===========================================================================
// ISystem — Style 2: NO Lookup at all. The aspect is assembled directly from
// chunk-iterated Execute parameters. Optional Boost arrives via
// EnabledRefRO-style optional matching is not available for plain optional
// components in IJobEntity, so here the aspect simply leaves Boost unbound
// (HasBoost == false). If you need optional lookups on top, add a Lookup
// with only the optional slots requested.
// ===========================================================================
[BurstCompile]
public partial struct ExamplePrimedSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ExampleTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new ExamplePrimedJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule();
    }

    [BurstCompile]
    [WithAll(typeof(ExampleTag))]
    partial struct ExamplePrimedJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(Entity entity,
                     RefRO<ExampleSpeed> speed,
                     RefRW<ExamplePosition> position,
                     RefRW<ExampleLevel> level,
                     DynamicBuffer<ExampleWaypoint> waypoints)
        {
            var aspect = new ExampleAspect
            {
                Entity = entity,
                Speed = speed,
                Position = position,
                LevelRW = level,
                Waypoints = waypoints,
                // Boost not bound -> HasBoost is false
            };

            aspect.MoveAlongWaypoints(DeltaTime);
        }
    }
}

// ===========================================================================
// ISystem — Style 3: mixed / partial. Iterated components come from Execute
// parameters; the system owns ONLY the slots it needs beyond that — here a
// single bare LookupSlot for the optional Boost. (For several slots, bundle
// them in a SlotGroup<...> so one Update call covers them.) No unrequested
// container ever enters job data, so no safety exemptions are needed.
// ===========================================================================
[BurstCompile]
public partial struct ExampleMixedSystem : ISystem
{
    LookupSlot<ExampleBoost> _boost;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _boost.Request(ref state, isReadOnly: true);

        state.RequireForUpdate<ExampleTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _boost.Update(ref state);

        new ExampleMixedJob
        {
            Boost = _boost,
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule();
    }

    [BurstCompile]
    [WithAll(typeof(ExampleTag))]
    partial struct ExampleMixedJob : IJobEntity
    {
        public LookupSlot<ExampleBoost> Boost;
        public float DeltaTime;

        void Execute(Entity entity,
                     RefRO<ExampleSpeed> speed,
                     RefRW<ExamplePosition> position,
                     RefRW<ExampleLevel> level,
                     DynamicBuffer<ExampleWaypoint> waypoints)
        {
            var aspect = new ExampleAspect
            {
                Entity = entity,
                Speed = speed,
                Position = position,
                LevelRW = level,
                Waypoints = waypoints,
                Boost = Boost.BindROOptional(entity),
            };

            aspect.MoveAlongWaypoints(DeltaTime);
        }
    }
}

// ===========================================================================
// SystemBase — managed system, main-thread usage.
// ===========================================================================
public partial class ExampleManagedSystem : SystemBase
{
    ExampleAspect.Lookup _lookup;
    Entity _selected;

    protected override void OnCreate()
    {
        _lookup.Initialize(this);
        _lookup.Speed.Request(this, isReadOnly: true);
        _lookup.Position.Request(this, isReadOnly: false);
        _lookup.Level.Request(this, isReadOnly: false);
        _lookup.Boost.Request(this, isReadOnly: true);
        _lookup.Waypoints.Request(this, isReadOnly: false);
    }

    protected override void OnUpdate()
    {
        _lookup.Update(this);

        if (_selected != Entity.Null)
        {
            var aspect = _lookup[_selected];
            aspect.LevelUpAndAddPatrolPoint(new float3(1, 0, 1));
        }
    }
}