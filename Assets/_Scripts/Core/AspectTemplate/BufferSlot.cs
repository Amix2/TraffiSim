using Unity.Entities;

// ---------------------------------------------------------------------------
// BufferSlot<T> — the DynamicBuffer counterpart of LookupSlot<T>.
// Read-only enforcement comes from how the slot was requested: a buffer
// obtained through a read-only lookup throws the built-in safety exception on
// any write (Add, RemoveAt, ElementAt, ...).
// ---------------------------------------------------------------------------
public struct BufferSlot<T> : ISlot where T : unmanaged, IBufferElementData
{
    BufferLookup<T> _lookup;
    bool _readOnly;

    public void Request(ref SystemState state, bool isReadOnly)
    {
        _readOnly = isReadOnly;
        _lookup = state.GetBufferLookup<T>(isReadOnly);
    }

    /// SystemBase / managed system variant.
    public void Request(ComponentSystemBase system, bool isReadOnly)
    {
        _readOnly = isReadOnly;
        _lookup = system.GetBufferLookup<T>(isReadOnly);
    }

    /// Call every frame before binding.
    public void Update(ref SystemState state) => _lookup.Update(ref state);

    /// SystemBase / managed system variant.
    public void Update(SystemBase system) => _lookup.Update(system);

    public readonly bool Has(Entity e) => _lookup.HasBuffer(e);

    public readonly bool IsReadOnly => _readOnly;

    /// Throws (built-in) if the entity lacks the buffer.
    public DynamicBuffer<T> Bind(Entity e) => _lookup[e];

    /// Optional buffer: default (not created) when the entity lacks it.
    public DynamicBuffer<T> BindOptional(Entity e)
        => _lookup.HasBuffer(e) ? _lookup[e] : default;

    /// Enableable buffers.
    public readonly bool IsEnabled(Entity e) => _lookup.IsBufferEnabled(e);

    public void SetEnabled(Entity e, bool value) => _lookup.SetBufferEnabled(e, value);
}