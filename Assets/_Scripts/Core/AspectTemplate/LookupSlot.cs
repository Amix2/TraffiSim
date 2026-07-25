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
// systems may request only the subset of an aspect they use. Accessing an
// unbound field throws the built-in invalid-ref safety exception.
// ---------------------------------------------------------------------------
public struct LookupSlot<T> : ISlot where T : unmanaged, IComponentData
{
    ComponentLookup<T> _lookup;
    bool _requested;

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
        _lookup = state.GetComponentLookup<T>(isReadOnly);
    }

    /// SystemBase / managed system variant.
    public void Request(ComponentSystemBase system, bool isReadOnly)
    {
        _requested = true;
        _lookup = system.GetComponentLookup<T>(isReadOnly);
    }

    /// Call every frame before binding. Updates unconditionally so
    /// initialized-but-unrequested slots never go stale as job data.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    /// False when the slot was never requested.
    /// NOTE: true for a component that is present but DISABLED — enabled
    /// state is separate from presence. Use IsEnabled for that.
    public readonly bool Has(Entity e) => _requested && _lookup.HasComponent(e);

    // Exposed for the enableable extensions below (which need a stronger
    // type constraint than this struct can declare).
    public readonly ComponentLookup<T> Lookup => _lookup;
    public readonly bool Requested => _requested;

    // -----------------------------------------------------------------------
    // Eager binds — called once, in the aspect-creating indexer. Every bind
    // returns default (an invalid ref) when the slot was never requested.
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
    // Enableable components. The enable bit lives in a per-chunk bitmask, not
    // in the component data, so it needs its own handle: EnabledRefRO/RW.
    // Pass T itself as TEnableable (C# can't constrain T to IEnableableComponent
    // here without a separate slot type). All of these throw at runtime if T
    // is not enableable.
    //
    // NOTE: enabled state is INDEPENDENT of presence — HasComponent() is true
    // for a disabled component, and its data stays readable/writable.
    // Enabling/disabling is not a structural change, so bound refs stay valid.
    // -----------------------------------------------------------------------

    public EnabledRefRO<TEnableable> BindEnabledRO<TEnableable>(Entity e)
        where TEnableable : unmanaged, IComponentData, IEnableableComponent
        => _requested ? _lookup.GetEnabledRefRO<TEnableable>(e) : default;

    public EnabledRefRW<TEnableable> BindEnabledRW<TEnableable>(Entity e)
        where TEnableable : unmanaged, IComponentData, IEnableableComponent
        => _requested ? _lookup.GetEnabledRefRW<TEnableable>(e) : default;

    /// Optional enableable component: invalid handle when the entity lacks it.
    public EnabledRefRO<TEnableable> BindEnabledROOptional<TEnableable>(Entity e)
        where TEnableable : unmanaged, IComponentData, IEnableableComponent
        => _requested && _lookup.HasComponent(e) ? _lookup.GetEnabledRefRO<TEnableable>(e) : default;

    /// Optional enableable component, read-write variant.
    public EnabledRefRW<TEnableable> BindEnabledRWOptional<TEnableable>(Entity e)
        where TEnableable : unmanaged, IComponentData, IEnableableComponent
        => _requested && _lookup.HasComponent(e) ? _lookup.GetEnabledRefRW<TEnableable>(e) : default;

    /// Direct bit access, no handle. False when the slot was not requested.
    public readonly bool IsEnabled(Entity e)
        => _requested && _lookup.IsComponentEnabled(e);

    /// Requires an RW-requested slot.
    public void SetEnabled(Entity e, bool value)
        => _lookup.SetComponentEnabled(e, value);
}

// ---------------------------------------------------------------------------
// Enableable-component support. Extension methods because IEnableableComponent
// is a stronger constraint than LookupSlot<T> itself declares.
//
// Enabled state is NOT presence: a disabled component is still on the entity,
// its data intact, and HasComponent/BindRO/BindRW all succeed on it. Toggling
// is not a structural change, so refs and lookups stay valid.
//
// Reading enabled state needs only an RO request; writing it requires the
// slot to be requested with isReadOnly: false.
// ---------------------------------------------------------------------------
public static class EnableableSlotExtensions
{
    public static bool IsEnabled<T>(this in LookupSlot<T> slot, Entity e)
        where T : unmanaged, IComponentData, IEnableableComponent
        => slot.Requested && slot.Lookup.IsComponentEnabled(e);

    public static void SetEnabled<T>(this in LookupSlot<T> slot, Entity e, bool value)
        where T : unmanaged, IComponentData, IEnableableComponent
        => slot.Lookup.SetComponentEnabled(e, value);

    /// Bind enabled state as a self-contained handle (an aspect field).
    /// Invalid when the slot was never requested.
    public static EnabledRefRO<T> BindEnabledRO<T>(this in LookupSlot<T> slot, Entity e)
        where T : unmanaged, IComponentData, IEnableableComponent
        => slot.Requested ? slot.Lookup.GetEnabledRefRO<T>(e) : default;

    /// Read-write variant; needs an RW request.
    public static EnabledRefRW<T> BindEnabledRW<T>(this in LookupSlot<T> slot, Entity e)
        where T : unmanaged, IComponentData, IEnableableComponent
        => slot.Requested ? slot.Lookup.GetEnabledRefRW<T>(e) : default;

}