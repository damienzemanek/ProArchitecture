using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ProArchitecture.Data;

/// <summary>
/// A memory-aligned, 1-byte boolean for unmanaged and blittable contexts.
/// Solves the size ambiguity of the standard C# 'bool' (which can be 4 bytes) in structs.
///
/// Features/Configuration:
/// - Memory Efficiency: Strictly 1 byte, allowing for predictable struct packing.
/// - Blittable: Safe for use in unmanaged memory, NativeLists, and across P/Invoke boundaries.
/// - Inspector Friendly: Custom drawer support via ByteBool (shows as a standard toggle).
///
/// Usage:
/// - Replace 'bool' with 'ByteBool' in any struct destined for Data<T> or UnsafeList.
/// - Supports implicit conversion to and from standard 'bool'.
/// - Use .active for property access or .Set() for direct mutation.
/// </summary>
[Serializable, InlineProperty]
public struct ByteBool
{
    [SerializeField, HideInInspector] byte value;

    [ShowInInspector, HideLabel]
    public bool active
    {
        get => value != 0;
        set => this.value = (byte)(value ? 1 : 0);
    }
        
    public void Set(bool _value) => value = (byte)(_value ? 1 : 0);
    public static implicit operator bool(ByteBool b) => b.active;
    public static implicit operator ByteBool(bool b) => new ByteBool { value = (byte)(b ? 1 : 0) };
    public ByteBool(bool _value) => value = (byte)(_value ? 1 : 0);
}