using System;
using Unity.Entities;

// ---------------------------------------------------------------------------
// EnableableSlot<T> — slot for the ENABLE BIT of an enableable component.
//
// Separate from LookupSlot<T> because ComponentLookup<T> cannot constrain T
// to IEnableableComponent. A component may legitimately have both a
// LookupSlot (data) and an EnableableSlot (bit) in the same system.
//
// NOTE: enabled state is INDEPENDENT of presence — HasComponent() is true for
// a disabled component and its data stays readable/writable. Toggling is not
// a structural change, so bound refs survive it.
// ---------------------------------------------------------------------------
public struct EnableableSlot<T> : ISlot
    where T : unmanaged, IComponentData, IEnableableComponent
{
    private ComponentLookup<T> _lookup;
    private bool _readOnly;

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

    /// Call every frame before binding.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    /// Component presence — NOT the enable state.
    public readonly bool Has(Entity e) => _lookup.HasComponent(e);

    public readonly bool IsReadOnly => _readOnly;

    // -----------------------------------------------------------------------
    // Binds
    // -----------------------------------------------------------------------

    public EnabledRefRO<T> BindRO(Entity e) => _lookup.GetEnabledRefRO<T>(e);

    public EnabledRefRW<T> BindRW(Entity e)
    {
        ThrowIfReadOnly();
        return _lookup.GetEnabledRefRW<T>(e);
    }

    public EnabledRefRO<T> BindROOptional(Entity e)
        => _lookup.HasComponent(e) ? _lookup.GetEnabledRefRO<T>(e) : default;

    public EnabledRefRW<T> BindRWOptional(Entity e)
    {
        ThrowIfReadOnly();
        return _lookup.HasComponent(e) ? _lookup.GetEnabledRefRW<T>(e) : default;
    }

    /// Tolerant bind for AspectEnabledRef<T> fields — uses the slot's mode.
    public AspectEnabledRef<T> BindRef(Entity e)
        => new AspectEnabledRef<T>(_lookup, e, _readOnly);

    /// Tolerant bind with an explicit mode.
    public AspectEnabledRef<T> BindRef(Entity e, bool isReadOnly)
    {
        if (!isReadOnly) ThrowIfReadOnly();
        return new AspectEnabledRef<T>(_lookup, e, isReadOnly);
    }

    public AspectEnabledRef<T> BindRefOptional(Entity e)
        => _lookup.HasComponent(e) ? new AspectEnabledRef<T>(_lookup, e, _readOnly) : default;

    public AspectEnabledRef<T> BindRefOptional(Entity e, bool isReadOnly)
    {
        if (!isReadOnly) ThrowIfReadOnly();
        return _lookup.HasComponent(e) ? new AspectEnabledRef<T>(_lookup, e, isReadOnly) : default;
    }

    /// Direct bit access, no handle.
    public readonly bool IsEnabled(Entity e) => _lookup.IsComponentEnabled(e);

    /// Requires a read-write slot.
    public void SetEnabled(Entity e, bool value)
    {
        ThrowIfReadOnly();
        _lookup.SetComponentEnabled(e, value);
    }

    private readonly void ThrowIfReadOnly()
    {
        if (_readOnly)
            throw new InvalidOperationException(
                $"EnableableSlot<{typeof(T)}> was requested read-only and cannot provide read-write access. Request it with isReadOnly: false.");
    }
}