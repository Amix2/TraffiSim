using NUnit.Framework.Constraints;
using System;
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
// LookupSlot<T> — a ComponentLookup<T> that remembers the access mode it was
// requested with.
//
// Systems declare only the slots they use, as their own fields:
//
//     [ReadOnly] public LookupSlot<RoadPortData> PortData;   // job field
//     PortData.Request(ref state, isReadOnly: true);         // OnCreate
//     PortData.Update(ref state);                            // OnUpdate
//
// A slot exists in job data only because someone declared it, so a forgotten
// Request surfaces as the built-in schedule-time container validation error,
// naming the field. Per-slot declaration also means per-slot [ReadOnly] and
// [NativeDisableParallelForRestriction], and no aliasing with components the
// job takes as Execute parameters.
// ---------------------------------------------------------------------------
public struct LookupSlot<T> : ISlot where T : unmanaged, IComponentData
{
    ComponentLookup<T> _lookup;
    bool _readOnly;

    public void Request(ref SystemState state, bool isReadOnly)
    {
        _readOnly = isReadOnly;
        _lookup = state.GetComponentLookup<T>(isReadOnly);
    }

    /// SystemBase / managed system variant.
    public void Request(ComponentSystemBase system, bool isReadOnly)
    {
        _readOnly = isReadOnly;
        _lookup = system.GetComponentLookup<T>(isReadOnly);
    }

    public static implicit operator ComponentLookup<T>(LookupSlot<T> slot) { return slot._lookup; }


    /// Call every frame before binding.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    public readonly bool Has(Entity e) => _lookup.HasComponent(e);

    /// True when the slot was requested read-only.
    public readonly bool IsReadOnly => _readOnly;

    // -----------------------------------------------------------------------
    // Binds
    // -----------------------------------------------------------------------

    /// Works on a slot requested either way.
    public RefRO<T> BindRO(Entity e) => _lookup.GetRefRO(e);

    /// Throws if the slot was requested read-only.
    public RefRW<T> BindRW(Entity e)
    {
        ThrowIfReadOnly();
        return _lookup.GetRefRW(e);
    }

    /// Optional component: invalid ref when the entity lacks it.
    public RefRO<T> BindROOptional(Entity e)
        => _lookup.HasComponent(e) ? _lookup.GetRefRO(e) : default;

    public RefRW<T> BindRWOptional(Entity e)
    {
        ThrowIfReadOnly();
        return _lookup.HasComponent(e) ? _lookup.GetRefRW(e) : default;
    }

    /// Tolerant bind for AspectRef<T> fields — uses the slot's own mode.
    public AspectRef<T> BindRef(Entity e)
        => new AspectRef<T>(_lookup, e, _readOnly);

    /// Tolerant bind with an explicit mode. Passing isReadOnly: true on a
    /// read-write slot is a deliberate downgrade; the reverse throws.
    public AspectRef<T> BindRef(Entity e, bool isReadOnly)
    {
        if (!isReadOnly) ThrowIfReadOnly();
        return new AspectRef<T>(_lookup, e, isReadOnly);
    }

    public AspectRef<T> BindRefOptional(Entity e)
        => _lookup.HasComponent(e) ? new AspectRef<T>(_lookup, e, _readOnly) : default;

    public AspectRef<T> BindRefOptional(Entity e, bool isReadOnly)
    {
        if (!isReadOnly) ThrowIfReadOnly();
        return _lookup.HasComponent(e) ? new AspectRef<T>(_lookup, e, isReadOnly) : default;
    }

    readonly void ThrowIfReadOnly()
    {
        if (_readOnly)
            throw new InvalidOperationException(
                $"LookupSlot<{typeof(T)}> was requested read-only and cannot provide read-write access. Request it with isReadOnly: false.");
    }
}