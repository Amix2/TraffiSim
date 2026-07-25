using Unity.Entities;

// ---------------------------------------------------------------------------
// BufferSlot<T> — the DynamicBuffer counterpart of LookupSlot<T>.
// Same Initialize / Request / Update lifecycle; see LookupSlot for details.
// ---------------------------------------------------------------------------
public struct BufferSlot<T> : ISlot where T : unmanaged, IBufferElementData
{
    BufferLookup<T> _lookup;
    bool _requested;

    /// Constructs the slot read-only WITHOUT marking it requested.
    public void Initialize(ref SystemState state)
        => _lookup = state.GetBufferLookup<T>(isReadOnly: true);

    /// SystemBase / managed system variant.
    public void Initialize(ComponentSystemBase system)
        => _lookup = system.GetBufferLookup<T>(isReadOnly: true);

    public void Request(ref SystemState state, bool isReadOnly)
    {
        _requested = true;
        _lookup = state.GetBufferLookup<T>(isReadOnly);
    }

    /// SystemBase / managed system variant.
    public void Request(ComponentSystemBase system, bool isReadOnly)
    {
        _requested = true;
        _lookup = system.GetBufferLookup<T>(isReadOnly);
    }

    /// Call every frame before binding. Updates unconditionally.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    /// False when the slot was never requested.
    /// NOTE: true for a buffer that is present but DISABLED.
    public readonly bool Has(Entity e) => _requested && _lookup.HasBuffer(e);

    // Exposed for the enableable extensions below.
    public readonly BufferLookup<T> Lookup => _lookup;
    public readonly bool Requested => _requested;

    /// Throws (built-in) if the entity lacks the buffer. Returns a default
    /// (not created) buffer when the slot was never requested.
    public DynamicBuffer<T> Bind(Entity e)
        => _requested ? _lookup[e] : default;

    /// Optional buffer. Also returns a default buffer when the entity lacks
    /// it — gate access with buffer.IsCreated (or an aspect Has property).
    public DynamicBuffer<T> BindOptional(Entity e)
        => _requested && _lookup.HasBuffer(e) ? _lookup[e] : default;

    // -----------------------------------------------------------------------
    // Enableable buffers. Throws at runtime if T is not enableable.
    // Enabled state is independent of presence; the elements stay intact.
    // -----------------------------------------------------------------------

    /// False when the slot was not requested.
    public readonly bool IsEnabled(Entity e)
        => _requested && _lookup.IsBufferEnabled(e);

    /// Requires an RW-requested slot.
    public void SetEnabled(Entity e, bool value)
        => _lookup.SetBufferEnabled(e, value);
}

// ---------------------------------------------------------------------------
// Enableable-buffer support. Same rules as components: disabled != absent,
// toggling is not a structural change, writing needs an RW request.
// ---------------------------------------------------------------------------
public static class EnableableBufferSlotExtensions
{
    public static bool IsEnabled<T>(this in BufferSlot<T> slot, Entity e)
        where T : unmanaged, IBufferElementData, IEnableableComponent
        => slot.Requested && slot.Lookup.IsBufferEnabled(e);

    public static void SetEnabled<T>(this in BufferSlot<T> slot, Entity e, bool value)
        where T : unmanaged, IBufferElementData, IEnableableComponent
        => slot.Lookup.SetBufferEnabled(e, value);
}