using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProArchitecture.Data;

/// <summary>
/// A tiny handle for a single unmanaged value allocated on the unmanaged heap.
/// Provides stable pointer access that survives struct-copying (pass-by-value).
///
/// Features/Configuration:
/// - Pointer Stability: The data resides at a fixed address; copying the handle does not copy the data.
/// - Life-cycle Sharing: Multiple handles point to the same ByteBool or state flag.
/// - Performance: Raw pointer access via .Ref or .Ptr; zero overhead beyond the allocation.
///
/// Usage:
/// - Initialize: var handle = new MallocHandle<int>(10, Allocator.Persistent);
/// - Access: int val = handle.Ref; or *handle.Ptr = 20;
/// - Cleanup: MUST call .Free() when the last owner is finished.
///
/// Validation / Exception Handling:
/// - Implicit conversion allows the handle to be used as the underlying type in many expressions.
/// - Requires manual memory management (Malloc/Free).
/// </summary>
public unsafe struct MallocHandle<T> where T : unmanaged
{
    public static implicit operator T(MallocHandle<T> handle) => handle.Ref;
    public static implicit operator MallocHandle<T>(in T value) => new(value);
        
    public ref T Ref => ref *Ptr;
    public T* Ptr;
    Allocator allocation;
    public bool IsCreated => Ptr != null;
    public MallocHandle(T initValue, Allocator allocator = Allocator.Persistent)
    {
        Ptr = (T*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<T>(), 
            UnsafeUtility.AlignOf<T>(),
            allocation = allocator);
            
        UnsafeUtility.WriteArrayElement(Ptr, 0, initValue);
    }
    public void Free() => UnsafeUtility.Free(Ptr, allocation);
}