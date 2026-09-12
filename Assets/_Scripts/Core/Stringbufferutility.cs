using System;
using System.Diagnostics;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

/// <summary>
/// Stores UTF-8 text inside a DynamicBuffer whose element type is a 1-byte struct.
/// The element type is generic so you can have several distinct string buffers on the
/// same entity (JsonText, DebugName, ...) without them colliding.
///
/// Example element:
///     public struct JsonByte : IBufferElementData { public byte Value; }
///
/// Requires "Allow unsafe code" on the assembly definition.
/// </summary>
public static class StringBufferUtility
{
    // ---------------------------------------------------------------- create

    /// <summary>Adds (or reuses) the buffer on the entity and fills it with <paramref name="text"/>.</summary>
    public static DynamicBuffer<T> MakeBuffer<T>(EntityManager entityManager, Entity entity, string text)
        where T : unmanaged, IBufferElementData
    {
        DynamicBuffer<T> buffer = entityManager.HasBuffer<T>(entity)
            ? entityManager.GetBuffer<T>(entity)
            : entityManager.AddBuffer<T>(entity);

        SetString(buffer, text);
        return buffer;
    }

    /// <summary>Command-buffer variant, for use inside systems.</summary>
    public static DynamicBuffer<T> MakeBuffer<T>(EntityCommandBuffer ecb, Entity entity, string text)
        where T : unmanaged, IBufferElementData
    {
        DynamicBuffer<T> buffer = ecb.AddBuffer<T>(entity);
        SetString(buffer, text);
        return buffer;
    }

    // ----------------------------------------------------------------- write

    /// <summary>Replaces the entire contents of an existing buffer with <paramref name="text"/>.</summary>
    public static unsafe void SetString<T>(DynamicBuffer<T> buffer, string text)
        where T : unmanaged, IBufferElementData
    {
        CheckElementSize<T>();

        DynamicBuffer<byte> bytes = buffer.Reinterpret<byte>();

        if (string.IsNullOrEmpty(text))
        {
            bytes.Clear();
            return;
        }

        Encoding utf8 = Encoding.UTF8;

        fixed (char* chars = text)
        {
            int byteCount = utf8.GetByteCount(chars, text.Length);

            // Resize first — this may reallocate, so the pointer is taken afterwards.
            bytes.ResizeUninitialized(byteCount);
            utf8.GetBytes(chars, text.Length, (byte*)bytes.GetUnsafePtr(), byteCount);
        }
    }

    /// <summary>
    /// Copies raw UTF-8 bytes straight into the buffer. Use this for text coming from
    /// native code — it skips the managed string entirely.
    /// </summary>
    public static unsafe void SetBytes<T>(DynamicBuffer<T> buffer, byte* source, int length)
        where T : unmanaged, IBufferElementData
    {
        CheckElementSize<T>();

        DynamicBuffer<byte> bytes = buffer.Reinterpret<byte>();

        if (source == null || length <= 0)
        {
            bytes.Clear();
            return;
        }

        bytes.ResizeUninitialized(length);
        UnsafeUtility.MemCpy(bytes.GetUnsafePtr(), source, length);
    }

    // ------------------------------------------------------------------ read

    /// <summary>Decodes the buffer back into a managed string. Allocates; not Burst-compatible.</summary>
    public static unsafe string GetString<T>(DynamicBuffer<T> buffer)
        where T : unmanaged, IBufferElementData
    {
        CheckElementSize<T>();

        DynamicBuffer<byte> bytes = buffer.Reinterpret<byte>();
        if (bytes.Length == 0)
            return string.Empty;

        return Encoding.UTF8.GetString((byte*)bytes.GetUnsafeReadOnlyPtr(), bytes.Length);
    }

    /// <summary>Convenience overload that looks the buffer up on the entity.</summary>
    public static string GetString<T>(EntityManager entityManager, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!entityManager.HasBuffer<T>(entity))
            return string.Empty;

        return GetString(entityManager.GetBuffer<T>(entity, isReadOnly: true));
    }

    /// <summary>
    /// Read-only pointer to the UTF-8 bytes, for parsers that consume UTF-8 directly
    /// (Unity.Serialization.Json, custom tokenizers) without materialising a string.
    /// </summary>
    public static unsafe byte* GetUtf8Ptr<T>(DynamicBuffer<T> buffer, out int length)
        where T : unmanaged, IBufferElementData
    {
        CheckElementSize<T>();

        DynamicBuffer<byte> bytes = buffer.Reinterpret<byte>();
        length = bytes.Length;
        return length == 0 ? null : (byte*)bytes.GetUnsafeReadOnlyPtr();
    }

    // ----------------------------------------------------------------- guard

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    static void CheckElementSize<T>() where T : unmanaged
    {
        if (UnsafeUtility.SizeOf<T>() != 1)
        {
            throw new InvalidOperationException(
                $"{typeof(T)} is {UnsafeUtility.SizeOf<T>()} bytes. " +
                "String buffer elements must be exactly 1 byte — wrap a single byte field, " +
                "e.g. 'public struct JsonByte : IBufferElementData { public byte Value; }'.");
        }
    }
}