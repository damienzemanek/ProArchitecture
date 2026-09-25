using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProArchitecture.Data;

/// <summary>
/// Global safety net and lifecycle manager for all unmanaged Data handles.
/// Tracks malloc'd lifecycle flags to prevent memory leaks during Unity Domain reloads.
///
/// Features/Configuration:
/// - Automated Cleanup: Hooks into AppDomain.DomainUnload to free leaked unmanaged memory.
/// - Global Invalidation: Forcefully sets IsActive to false for all registered handles on shutdown.
/// - Performance: Registration only occurs during Allocation/Disposal; zero impact on evaluation loops.
/// - Self-Initializing: Transparently sets up the internal UnsafeList on first registration.
///
/// Usage:
/// - Internal use only: Data<T> calls Register() in constructor and Unregister() in Dispose().
/// - Handles cleanup of 'Persistent' allocations that would otherwise survive stopping the Editor.
///
/// Validation / Exception Handling:
/// - Uses RemoveAtSwapBack for O(1) unregistration (order not preserved).
/// - Safe for multi-call initialization via internal 'initialized' flag.
/// </summary>
public static unsafe class DataActiveRegistry
{
    const int INITIAL_DATACAPACITY = 1024;
    
    static UnsafeList<IntPtr> activeDatas;
    static bool initialized;

    static void Init()
    {
        if (initialized) return;
        activeDatas = new UnsafeList<IntPtr>(INITIAL_DATACAPACITY, Allocator.Persistent);
        initialized = true;

        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
    }

    public static void Register(ByteBool* ptr)
    {
        if(!initialized) Init();
        activeDatas.Add((IntPtr)ptr);
    }
    

    public static void Unregister(ByteBool* ptr)
    {
        if (!initialized) return;
        for (int i = 0; i < activeDatas.Length; i++)
        {
            if (activeDatas[i] != (IntPtr)ptr) continue;
            activeDatas.RemoveAtSwapBack(i);
            break;
        }
    }

    public static void OnDomainUnload(object sender, EventArgs e)
    {
        if (!initialized) return;

        for (int i = 0; i < activeDatas.Length; i++)
        {
            ByteBool* ptr = (ByteBool*)activeDatas[i];
            if (ptr == null) continue;
            ptr->Set(false); // Invalidate all existing handles
            UnsafeUtility.Free(ptr, Allocator.Persistent);
        }
        
        activeDatas.Dispose();
        initialized = false;
    }
}