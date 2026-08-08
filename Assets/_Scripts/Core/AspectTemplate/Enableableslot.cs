using Unity.Entities;

// ---------------------------------------------------------------------------
// EnableableSlot<T> — slot for the ENABLE BIT of an enableable component.
//
// Separate from LookupSlot<T> because ComponentLookup<T> cannot constrain T
// to IEnableableComponent; declaring the constraint on the slot type removes
// the extra type argument the bind methods used to need.
//
// The enable bit lives in a per-chunk bitmask, not in the component data, so
// a component can legitimately have BOTH a LookupSlot (data) and an
// EnableableSlot (bit) in the same aspect.
//
// NOTE: enabled state is INDEPENDENT of presence — HasComponent() is true for
// a disabled component and its data stays readable/writable. Toggling is not
// a structural change, so bound refs survive it.
//
// Bind kind must match the aspect's field type:
//   EnabledRefRO<T>     -> BindRO    (slot may be RO or RW)
//   EnabledRefRW<T>     -> BindRW    (slot MUST be RW)
//   AspectEnabledRef<T> -> BindRef   (tolerant: either mode)
// ---------------------------------------------------------------------------
public struct EnableableSlot<T> : ISlot
    where T : unmanaged, IComponentData, IEnableableComponent
{
    ComponentLookup<T> _lookup;
    bool _requested;
    bool _readOnly;

    /// Constructs the slot read-only WITHOUT marking it requested.
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

    /// Call every frame before binding.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    /// Component presence — NOT the enable state.
    public readonly bool Has(Entity e) => _requested && _lookup.HasComponent(e);

    /// True when the slot was requested read-only.
    public readonly bool IsReadOnly => _readOnly;

    // -----------------------------------------------------------------------
    // Binds. Every bind returns default (invalid) when the slot was never
    // requested.
    // -----------------------------------------------------------------------

    public EnabledRefRO<T> BindRO(Entity e)
        => _requested ? _lookup.GetEnabledRefRO<T>(e) : default;

    public EnabledRefRW<T> BindRW(Entity e)
        => _requested ? _lookup.GetEnabledRefRW<T>(e) : default;

    public EnabledRefRO<T> BindROOptional(Entity e)
        => _requested && _lookup.HasComponent(e) ? _lookup.GetEnabledRefRO<T>(e) : default;

    public EnabledRefRW<T> BindRWOptional(Entity e)
        => _requested && _lookup.HasComponent(e) ? _lookup.GetEnabledRefRW<T>(e) : default;

    /// Tolerant bind — for AspectEnabledRef<T> fields. Reads always work;
    /// writes throw when the slot was requested read-only.
    public AspectEnabledRef<T> BindRef(Entity e)
        => _requested
            ? new AspectEnabledRef<T>(_lookup.GetEnabledRefRO<T>(e),
                                      _readOnly ? default : _lookup.GetEnabledRefRW<T>(e))
            : default;

    public AspectEnabledRef<T> BindRefOptional(Entity e)
        => _requested && _lookup.HasComponent(e)
            ? new AspectEnabledRef<T>(_lookup.GetEnabledRefRO<T>(e),
                                      _readOnly ? default : _lookup.GetEnabledRefRW<T>(e))
            : default;

    /// Direct bit access, no handle. False when the slot was not requested.
    public readonly bool IsEnabled(Entity e)
        => _requested && _lookup.IsComponentEnabled(e);

    /// Requires an RW-requested slot.
    public void SetEnabled(Entity e, bool value)
        => _lookup.SetComponentEnabled(e, value);
}