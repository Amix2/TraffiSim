using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

[BurstCompile]
public struct ComponentLookup<T1, T2>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
{
    public ComponentLookup<T1> Lookup1;
    public ComponentLookup<T2> Lookup2;

    public ComponentLookup(ComponentLookup<T1> l1, ComponentLookup<T2> l2)
    {
        Lookup1 = l1;
        Lookup2 = l2;
    }

    public ComponentLookup(ref SystemState state, bool is1ReadOnly, bool is2ReadOnly)
    {
        Lookup1 = state.GetComponentLookup<T1>(is1ReadOnly);
        Lookup2 = state.GetComponentLookup<T2>(is2ReadOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefRO<T> GetRefRO<T>(Entity e) where T : unmanaged, IComponentData
    {
        return Get<T>().GetRefRO(e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefRW<T> GetRefRW<T>(Entity e) where T : unmanaged, IComponentData
    {
        return Get<T>().GetRefRW(e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>(Entity e) where T : unmanaged, IComponentData
    {
        return Get<T>().HasComponent(e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentLookup<T> Get<T>() where T : unmanaged, IComponentData
    {
        if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<T1>())
            return UnsafeUtility.As<ComponentLookup<T1>, ComponentLookup<T>>(ref Lookup1);
        if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<T2>())
            return UnsafeUtility.As<ComponentLookup<T2>, ComponentLookup<T>>(ref Lookup2);

        return default; // unreachable with proper usage
    }
    /// <summary>
    /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
    /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
    /// type handle safe to use.
    /// </summary>
    /// <param name="systemState">The SystemState of the system on which this type handle is cached.</param>
    public void Update(ref SystemState systemState)
    {
        Lookup1.Update(ref systemState);
        Lookup2.Update(ref systemState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ComponentLookup<T2, T1>(ComponentLookup<T1, T2> source)
    {
        return new ComponentLookup<T2, T1>(source.Lookup2, source.Lookup1);
    }
}

[BurstCompile]
public struct BufferLookup<T1, T2>
    where T1 : unmanaged, IBufferElementData
    where T2 : unmanaged, IBufferElementData
{
    public BufferLookup<T1> Lookup1;
    public BufferLookup<T2> Lookup2;

    public BufferLookup(BufferLookup<T1> l1, BufferLookup<T2> l2)
    {
        Lookup1 = l1;
        Lookup2 = l2;
    }

    public BufferLookup(ref SystemState state, bool is1ReadOnly, bool is2ReadOnly)
    {
        Lookup1 = state.GetBufferLookup<T1>(is1ReadOnly);
        Lookup2 = state.GetBufferLookup<T2>(is2ReadOnly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DynamicBuffer<T> GetBuffer<T>(Entity e) where T : unmanaged, IBufferElementData
    {
        return Get<T>()[e];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>(Entity e) where T : unmanaged, IBufferElementData
    {
        return Get<T>().HasBuffer(e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferLookup<T> Get<T>() where T : unmanaged, IBufferElementData
    {
        if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<T1>())
            return UnsafeUtility.As<BufferLookup<T1>, BufferLookup<T>>(ref Lookup1);
        if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<T2>())
            return UnsafeUtility.As<BufferLookup<T2>, BufferLookup<T>>(ref Lookup2);

        return default; // unreachable with proper usage
    }
    /// <summary>
    /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
    /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
    /// type handle safe to use.
    /// </summary>
    /// <param name="systemState">The SystemState of the system on which this type handle is cached.</param>
    public void Update(ref SystemState systemState)
    {
        Lookup1.Update(ref systemState);
        Lookup2.Update(ref systemState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BufferLookup<T2, T1>(BufferLookup<T1, T2> source)
    {
        return new BufferLookup<T2, T1>(source.Lookup2, source.Lookup1);
    }
}