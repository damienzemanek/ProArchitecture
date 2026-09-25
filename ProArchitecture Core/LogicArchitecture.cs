using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ProArchitecture.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;



namespace ProArchitecture.Logic
{
    
    
    public static unsafe class LogicBuilder<T> where T : unmanaged
    {
        // Not using worker threads atm, if i were i would switch to lazy init and [ThreadStatic]
        public static StaticBuilder Static;
        public static DynamicBuilder Instance;
        static LogicBuilder()
        {
            Static = new StaticBuilder();
            Instance = new DynamicBuilder();
        }

        public class StaticBuilder
        {
            NativeList<IntPtr> buffer;
            NativeList<IntPtr> Buffer 
            {
                get 
                {
                    if (!buffer.IsCreated) buffer = new NativeList<IntPtr>(8, Allocator.Persistent);
                    return buffer;
                }
            }
            public void EnsureClear() => Buffer.Clear();
            public StaticBuilder Add(RefToStatic<Operation<T>> op)
            {
                Buffer.Add(op);
                return this;
            }
            public Logic<T> Build()
            {
                if (!buffer.IsCreated || buffer.Length == 0) return Logic<T>.EmptyStatic;
                
                int count = buffer.Length;
                int size = count * UnsafeUtility.SizeOf<IntPtr>();
                
                Operation<T>** ops = (Operation<T>**)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<IntPtr>(), Allocator.Persistent);
                UnsafeUtility.MemCpy(ops, buffer.GetUnsafePtr(), size);

                EnsureClear();
                return new Logic<T>(ops, count, true);
            }
        }

        public class DynamicBuilder 
        {
            NativeList<IntPtr> buffer;
            NativeList<IntPtr> Buffer 
            {
                get 
                {
                    if (!buffer.IsCreated) buffer = new NativeList<IntPtr>(8, Allocator.Persistent);
                    return buffer;
                }
            }
            public void EnsureClear() => Buffer.Clear();
            public DynamicBuilder Add(RefToStatic<Operation<T>> op)
            {
                Buffer.Add(op);
                return this;
            }
            public DynamicLogics<T> Build() 
            {
                Logic<T> logicValue = InternalBuild();
                // Allocate the Logic struct itself on the persistent heap
                Logic<T>* heapLogic = (Logic<T>*)UnsafeUtility.Malloc(
                    sizeof(Logic<T>), 
                    UnsafeUtility.AlignOf<Logic<T>>(), 
                    Allocator.Persistent
                );
                *heapLogic = logicValue; // Copy the struct to the heap
                return new DynamicLogics<T>(heapLogic);
            }
            Logic<T> InternalBuild()
            {
                if (!buffer.IsCreated || buffer.Length == 0) return Logic<T>.EmptyStatic;
                
                int count = buffer.Length;
                int size = count * UnsafeUtility.SizeOf<IntPtr>();
                
                Operation<T>** ops = (Operation<T>**)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<IntPtr>(), Allocator.Persistent);
                UnsafeUtility.MemCpy(ops, buffer.GetUnsafePtr(), size);

                EnsureClear();
                return new Logic<T>(ops, count, false);
            }
        }
    }
    
    /// <summary>
    /// Managed handle for a Logic instance residing on the unmanaged heap.
    /// Bridges the gap between persistent state logic and system-owned collections.
    ///
    /// Features/Configuration:
    /// - Persistent Lifetime: Moves <see cref="Logic{T}"/> from stack to persistent heap (Malloc) to prevent dangling pointers.
    /// - Auto-Cleanup: Automatically disables its entry in a <see cref="LogicGroup{T}"/> upon disposal via parent pointer tracking.
    /// - Slick API: Implicitly converts to `Ref<Logic<T>>`, allowing seamless integration with `LogicGroupBuilder`.
    /// - Performance: Direct pointer access to the heap-allocated Logic with zero managed overhead.
    ///
    /// Usage:
    /// - Creation: `LogicBuilder<T>.Instance.Add(opA).Build();` // Returns DynamicLogics<T>
    /// - Execution: `myDynLogic.TryRunAllSequentially(ref data);`
    /// - Cleanup: MUST call `Dispose()` to free the malloc'd Logic and its operation pointer array.
    ///
    /// Validation / Exception Handling:
    /// - Disposal is safe for repeated calls (idempotent).
    /// - Ensures the underlying operation array is not part of a shared 'Empty' logic before freeing.
    /// </summary>
    /// <typeparam name="T">Unmanaged data type the logic acts upon</typeparam>
    public unsafe struct DynamicLogics<T> : IDisposable
        where T : unmanaged
    {
        
        
        public int logicGroupIndex;
        public void* parentLogicsDataPtr;
        Logic<T>* _heapLogic;
        public Ref<Logic<T>> RefInstance => new(_heapLogic);
        public DynamicLogics(Logic<T>* logicPtr)
        {
            this._heapLogic = logicPtr;
            this.logicGroupIndex = -1;
            this.parentLogicsDataPtr = null;
        }
        
        public void TryRunAllSequentially(ref T data)
        {
            if (_heapLogic == null || _heapLogic->ops == null) return;
            _heapLogic->TryRunAllSequentially(ref data);
        }
        public void Dispose()
        {
            if (_heapLogic == null) return;

            // Auto-tick: Disable in parent group if registered
            if (parentLogicsDataPtr != null && logicGroupIndex >= 0)
            {
                var dataPtr = (Data<Ref<Logic<T>>, MtdLogicGroup>*)parentLogicsDataPtr;
                dataPtr->GetWrapper(logicGroupIndex).ActiveElement.Set(false);
            }

            // Free the persistent pointer array allocated by the builder
            // DO NOT free the shared Empty logic's memory
            if (_heapLogic->ops != null && _heapLogic->ops != Logic<T>.EmptyStatic.ops)
            {
                UnsafeUtility.Free(_heapLogic->ops, Allocator.Persistent);
            }
            // Free the Logic struct itself from the heap
            UnsafeUtility.Free(_heapLogic, Allocator.Persistent);
            _heapLogic = null;
        }
        
        public static implicit operator Ref<Logic<T>>(in DynamicLogics<T> logic) => logic.RefInstance;

        public static implicit operator Ref<DynamicLogics<T>>(in DynamicLogics<T> dynLogic)
        { 
            fixed(DynamicLogics<T>* ptr = &dynLogic)
                return new Ref<DynamicLogics<T>>(ptr);
        }

    }

    /// <summary>
    /// High-performance container for a sequence of Operations, stored as a double-indirected array of pointers.
    /// Designed for zero-allocation sequential execution and stable reference capturing.
    ///
    /// Features/Configuration:
    /// - Pointer Stability: Uses `Operation<T>**` to follow stable pointers to static or persistent memory.
    /// - Dual Life-cycles: Supports both `Static` (permanent/global) and `Dynamic` (instanced/heap) lifetimes.
    /// - Slick API: Implicitly converts to `RefToStatic`, enabling the "Just Add()" fluent builder pattern.
    /// - Zero GC Pressure: Execution and storage are entirely unmanaged; no managed heap traffic.
    ///
    /// Usage:
    /// - Creation: Use `LogicBuilder<T>.Static` or `LogicBuilder<T>.Instance`.
    /// - Batching: Optimized for `Batcher.Process` to run across entire `Data<T>` collections.
    /// - Individual Call: `logic.TryRun(ref data, index)` for targeted operation execution.
    ///
    /// Validation / Exception Handling:
    /// - Sequential execution safely handles null pointer arrays via early return.
    /// - Individual `TryRun` includes bounds checking with `ArgumentOutOfRangeException`.
    /// </summary>
    /// <typeparam name="T">Unmanaged data type the logic acts upon</typeparam>
    public readonly unsafe struct Logic<T> 
        where T : unmanaged
    {
        public static implicit operator Logic<T>(in Operation<T> op) => LogicBuilder<T>.Static.Add(op).Build();
        public static implicit operator Ref<Logic<T>>(in Logic<T> logic) => Implicit.Convert(logic).ToStaticRef().Ref;
        public static implicit operator RefToStatic<Logic<T>>(in Logic<T> logic) => Implicit.Convert(logic).ToStaticRef();
        
        
        public readonly ByteBool isStatic;
        public static Logic<T> EmptyStatic = new Logic<T>(null, 0, true);

        public bool hasOperations => count > 0;
        
        
        public readonly Operation<T>** ops;
        public readonly int count;

        public int Count => count;
        
        // Multi-Operation constructor used by the Builder
        public Logic(Operation<T>** _ops, int opCount, bool isStatic)
        {
            ops = _ops;
            count = opCount;
            this.isStatic = isStatic;
        }
        
        
        /// <summary>
        /// Use if you want to call all operations sequentially
        /// </summary>
        /// <param name="data"></param>
        public void TryRunAllSequentially(ref T data)
        {
            if (ops == null) return;
            for (int i = 0; i < count; i++)
            {
                // Double De-Reference
                var op = ops[i];
                if (op->ShouldRun(in data))
                {
                    op->Run(ref data);
                }
            }
        }
        
        /// <summary>
        /// Use if you want to call operations individually
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void TryRun(ref T data, int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            var op = ops[index];
            if (op->ShouldRun(in data))
                op->Run(ref data);
        }
    }
    
    
    
}

// Note another way to convert a ref struct into a pointer is by using
// (MyStruct*)UnsafeUtility.AddressOf(ref myStruct)
// To param in multiuple of the same ref struct use ReadonlySpan, and Memorymarshal its reference
// public readonly LogicOperation<T>* operations;    
// operations = (LogicOperation<T>*)UnsafeUtility.AddressOf(ref MemoryMarshal.GetReference(ops));      


