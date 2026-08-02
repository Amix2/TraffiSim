using Unity.Entities;

// ---------------------------------------------------------------------------
// AspectRef<T> — a ref field that tolerates EITHER access mode on its slot.
//
// Use it when the same aspect is consumed by systems that request the
// component read-only and systems that request it read-write:
//
//     public AspectRef<LocalTransform> LocalTransform;   // aspect field
//     lookup.LocalTransformLookup.Request(this, true);   // RO system
//     lookup.LocalTransformLookup.Request(this, false);  // RW system
//
//     LocalTransform.ValueRO.Position    // works in both
//     LocalTransform.ValueRW.Position    // RO slot -> built-in exception
//
// It holds both handles: RO is always bound, RW only when the slot was
// requested read-write. This is deliberately NOT a reinterpret of RefRO as
// RefRW — that would pass silently in release builds where safety checks are
// compiled out, turning a read-only violation into a data race.
// ---------------------------------------------------------------------------
public readonly struct AspectRef<T> where T : unmanaged, IComponentData
{
    readonly RefRO<T> _ro;
    readonly RefRW<T> _rw;

    public AspectRef(RefRO<T> ro, RefRW<T> rw)
    {
        _ro = ro;
        _rw = rw;
    }

    /// Builds from a lookup directly; caller states the lookup's access mode.
    public AspectRef(ComponentLookup<T> lookup, Entity e, bool isReadOnly)
    {
        _ro = lookup.GetRefRO(e);
        _rw = isReadOnly ? default : lookup.GetRefRW(e);
    }

    /// False when unbound (slot not requested, or component absent).
    public bool IsValid => _ro.IsValid;

    /// True only when the slot was requested read-write.
    public bool IsWritable => _rw.IsValid;

    public ref readonly T ValueRO => ref _ro.ValueRO;

    /// Throws the built-in invalid-ref exception when the slot was
    /// requested read-only.
    public ref T ValueRW => ref _rw.ValueRW;
}

// ---------------------------------------------------------------------------
// AspectEnabledRef<T> — the enable-bit counterpart of AspectRef<T>.
// ---------------------------------------------------------------------------
public readonly struct AspectEnabledRef<T>
    where T : unmanaged, IComponentData, IEnableableComponent
{
    readonly EnabledRefRO<T> _ro;
    readonly EnabledRefRW<T> _rw;

    public AspectEnabledRef(EnabledRefRO<T> ro, EnabledRefRW<T> rw)
    {
        _ro = ro;
        _rw = rw;
    }

    /// Builds from a lookup directly; caller states the lookup's access mode.
    public AspectEnabledRef(ComponentLookup<T> lookup, Entity e, bool isReadOnly)
    {
        _ro = lookup.GetEnabledRefRO<T>(e);
        _rw = isReadOnly ? default : lookup.GetEnabledRefRW<T>(e);
    }

    public bool IsValid => _ro.IsValid;
    public bool IsWritable => _rw.IsValid;

    public bool ValueRO => _ro.ValueRO;

    /// Setter throws the built-in invalid-ref exception when the slot was
    /// requested read-only.
    public bool ValueRW
    {
        get => _rw.ValueRW;
        set => _rw.ValueRW = value;
    }
}