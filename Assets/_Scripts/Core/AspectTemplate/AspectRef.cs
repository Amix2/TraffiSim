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
// It holds both handles. A RefRW alone is enough for full read+write: the
// RO handle is derived with RefRW's explicit RefRO conversion (which carries
// the safety handle through when checks are enabled), so ValueRO is a plain
// field access with no branch.
//
// The reverse is deliberately NOT done — reinterpreting a RefRO as a RefRW
// would pass silently in release builds where safety checks are compiled
// out, turning a read-only violation into a data race.
// ---------------------------------------------------------------------------
public readonly struct AspectRef<T> where T : unmanaged, IComponentData
{
    readonly RefRO<T> _ro;
    readonly RefRW<T> _rw;

    public AspectRef(RefRO<T> ro, RefRW<T> rw)
    {
        // Prefer the caller's RO handle; fall back to deriving it from RW so
        // passing only the RW ref still yields a readable AspectRef.
        _ro = ro.IsValid ? ro : (RefRO<T>)rw;
        _rw = rw;
    }

    /// Read-only: writes throw.
    public AspectRef(RefRO<T> ro)
    {
        _ro = ro;
        _rw = default;
    }

    /// Read-write: the RO handle is derived from it, so reads work too.
    public AspectRef(RefRW<T> rw)
    {
        _ro = (RefRO<T>)rw;
        _rw = rw;
    }

    /// Builds from a lookup directly; caller states the lookup's access mode.
    public AspectRef(ComponentLookup<T> lookup, Entity e, bool isReadOnly)
    {
        if (isReadOnly)
        {
            _ro = lookup.GetRefRO(e);
            _rw = default;
        }
        else
        {
            _rw = lookup.GetRefRW(e);
            _ro = (RefRO<T>)_rw;   // no second lookup call
        }
    }

    /// False when unbound (slot not requested, or component absent).
    /// _ro is always populated whenever anything is bound.
    public bool IsValid => _ro.IsValid;

    /// True only when a read-write handle is bound.
    public bool IsWritable => _rw.IsValid;

    public ref readonly T ValueRO => ref _ro.ValueRO;

    /// Throws the built-in invalid-ref exception when only a read-only
    /// handle is bound.
    public ref T ValueRW => ref _rw.ValueRW;
}

// ---------------------------------------------------------------------------
// AspectEnabledRef<T> — the enable-bit counterpart of AspectRef<T>.
// Keeps the read-through branch: unlike RefRW, EnabledRefRW has no
// conversion to EnabledRefRO. If your Entities version adds one, mirror the
// AspectRef approach above and drop the branch.
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

    /// Read-only: writes throw.
    public AspectEnabledRef(EnabledRefRO<T> ro)
    {
        _ro = ro;
        _rw = default;
    }

    /// Read-write: reads go through the RW handle, writes work.
    public AspectEnabledRef(EnabledRefRW<T> rw)
    {
        _ro = default;
        _rw = rw;
    }

    /// Builds from a lookup directly; caller states the lookup's access mode.
    public AspectEnabledRef(ComponentLookup<T> lookup, Entity e, bool isReadOnly)
    {
        _ro = lookup.GetEnabledRefRO<T>(e);
        _rw = isReadOnly ? default : lookup.GetEnabledRefRW<T>(e);
    }

    public bool IsValid => _ro.IsValid || _rw.IsValid;
    public bool IsWritable => _rw.IsValid;

    /// Reads through whichever handle is bound.
    public bool ValueRO => _ro.IsValid ? _ro.ValueRO : _rw.ValueRW;

    /// Setter throws the built-in invalid-ref exception when only a
    /// read-only handle is bound.
    public bool ValueRW
    {
        get => _rw.ValueRW;
        set => _rw.ValueRW = value;
    }
}