using Unity.Entities;

// ---------------------------------------------------------------------------
// ISlot — lets SlotGroup<> update heterogeneous slots generically.
// ---------------------------------------------------------------------------
public interface ISlot
{
    void Update(ref SystemState state);
    void Update(SystemBase system);
}

// ---------------------------------------------------------------------------
// LookupSlot<T> — a ComponentLookup<T> with an Initialize / Request / Update
// lifecycle and eager Bind methods.
//
// Lifecycle (in the system's OnCreate):
//   1. lookup.Initialize(ref state)  — constructs EVERY slot read-only, so
//      the whole Lookup is always valid job data (schedule-time container
//      validation passes, full safety checks stay on, no attributes).
//   2. slot.Request(ref state, ro)   — for each slot the system uses;
//      replaces the lookup with the desired access and marks it requested.
// Then in OnUpdate: lookup.Update(...) every frame, before binding.
//
// Initialized-but-unrequested slots are never fetched through: all binds
// return a default (invalid) ref when the slot was not requested, so
// systems may request only the subset of an aspect they use.
//
// Bind kind must match the aspect's field type:
//   RefRO<T>     -> BindRO    (slot may be RO or RW)
//   RefRW<T>     -> BindRW    (slot MUST be RW)
//   AspectRef<T> -> BindRef   (tolerant: either mode)
//
// Enable BITS live in EnableableSlot<T>, not here: ComponentLookup<T> cannot
// constrain T to IEnableableComponent. A component may have both slots.
// ---------------------------------------------------------------------------
public struct LookupSlot<T> : ISlot where T : unmanaged, IComponentData
{
    ComponentLookup<T> _lookup;
    bool _requested;
    bool _readOnly;

    /// Constructs the slot read-only WITHOUT marking it requested. Binds
    /// stay inert; this only keeps the container valid for job scheduling.
    public void Initialize(ref SystemState state)
        => _lookup = state.GetComponentLookup<T>(isReadOnly: true);

    /// SystemBase / managed system variant.
    public void Initialize(ComponentSystemBase system)
        => _lookup = system.GetComponentLookup<T>(isReadOnly: true);

    public void Request(ref SystemState state, bool isReadOnly)
    {
        _requested = true;
        _readOnly = isReadOnly;
        _lookup = state.GetComponentLookup<T>(isReadOnly);
    }

    /// SystemBase / managed system variant.
    public void Request(ComponentSystemBase system, bool isReadOnly)
    {
        _requested = true;
        _readOnly = isReadOnly;
        _lookup = system.GetComponentLookup<T>(isReadOnly);
    }

    /// Call every frame before binding. Updates unconditionally so
    /// initialized-but-unrequested slots never go stale as job data.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    /// False when the slot was never requested.
    public readonly bool Has(Entity e) => _requested && _lookup.HasComponent(e);

    /// True when the slot was requested read-only.
    public readonly bool IsReadOnly => _readOnly;

    // -----------------------------------------------------------------------
    // Fixed-access binds. Every bind returns default (invalid) when the slot
    // was never requested.
    // -----------------------------------------------------------------------

    /// Throws (built-in) if the entity lacks the component.
    public RefRO<T> BindRO(Entity e)
        => _requested ? _lookup.GetRefRO(e) : default;

    /// Throws (built-in) if the entity lacks the component or the slot was
    /// requested read-only.
    public RefRW<T> BindRW(Entity e)
        => _requested ? _lookup.GetRefRW(e) : default;

    /// Optional component. Also returns an invalid ref when the entity lacks
    /// it — gate access with ref.IsValid (or an aspect Has property).
    public RefRO<T> BindROOptional(Entity e)
        => _requested && _lookup.HasComponent(e) ? _lookup.GetRefRO(e) : default;

    /// Optional component, read-write variant.
    public RefRW<T> BindRWOptional(Entity e)
        => _requested && _lookup.HasComponent(e) ? _lookup.GetRefRW(e) : default;

    // -----------------------------------------------------------------------
    // Tolerant binds — for AspectRef<T> fields, which accept a slot requested
    // in EITHER mode. Reads always work; writes throw when the slot is RO.
    // -----------------------------------------------------------------------

    public AspectRef<T> BindRef(Entity e)
        => _requested
            ? new AspectRef<T>(_lookup.GetRefRO(e), _readOnly ? default : _lookup.GetRefRW(e))
            : default;

    public AspectRef<T> BindRefOptional(Entity e)
        => _requested && _lookup.HasComponent(e)
            ? new AspectRef<T>(_lookup.GetRefRO(e), _readOnly ? default : _lookup.GetRefRW(e))
            : default;

}