using System;
using ProArchitecture.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ProArchitecture.Logic
{
    
    public static unsafe class NativeListExtensions
    {
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeList<T> list) where T : unmanaged
            => new(list.GetUnsafeReadOnlyPtr(), list.Length);
    }

    public static unsafe class LogicGroupBuilder<T> where T : unmanaged
    {
        // Not using worker threads atm, if i were i would switch to lazy init and [ThreadStatic]
        public static StaticBuilder Static;
        public static DynamicBuilder Instance;
        static LogicGroupBuilder()
        {
            Static = new StaticBuilder();
            Instance = new DynamicBuilder();
        }

        public class StaticBuilder
        {
            NativeList<(RefToStatic<Logic<T>> logic, bool isInstance)> buffer;
            NativeList<(RefToStatic<Logic<T>> logic, bool isInstance)> Buffer
            {
                get
                {
                    if(!buffer.IsCreated) buffer = new NativeList<(RefToStatic<Logic<T>>, bool)>(8, Allocator.Persistent);
                    return buffer;
                }
            }

            public StaticBuilder Add(RefToStatic<Logic<T>> logic)
            {
                Buffer.Add((logic, false));
                return this;
            }
            
            public LogicGroup<T> Build(out int remainingCapacitySpace)
            {
                if(!buffer.IsCreated || buffer.Length == 0) throw new InvalidOperationException("Logics group is empty");
                var group = new LogicGroup<T>(buffer.AsReadOnlySpan(), buffer.Length, out remainingCapacitySpace);
                buffer.Clear();
                return group;
            }
        }

        public class DynamicBuilder
        {
            NativeList<(Ref<Logic<T>> logic, bool isInstance)> buffer;
            NativeList<(Ref<Logic<T>> logic, bool isInstance)> Buffer
            {
                get
                {
                    if(!buffer.IsCreated) buffer = new NativeList<(Ref<Logic<T>>, bool)>(8, Allocator.Persistent);
                    return buffer;
                }
            }
            public DynamicBuilder Add(RefToStatic<Logic<T>> logic)
            {
                Buffer.Add((logic.Ref, false));
                return this;
            }
            
            public DynamicBuilder Add(DynamicLogics<T> logic)
            {
                Buffer.Add((logic, true));
                return this;
            }


            public LogicGroup<T> Build(out int remainingCapacitySpace)
            {
                if(!buffer.IsCreated || buffer.Length == 0) throw new InvalidOperationException("Logics group is empty");
                var group = new LogicGroup<T>(buffer.AsReadOnlySpan(), buffer.Length, out remainingCapacitySpace);
                Debug.Log("[Dynamic LogicGroupBuilder] Group Current Size: " + group.LogicsData.currentSize + " Group Capacity: " + group.LogicsData.Capacity + "");
                buffer.Clear();
                return group;
            }
        }
    }
    
    
    
    public struct MtdLogicGroup
    {
        public ByteBool isDynamic;
    }
    
    /// <summary>
    /// Unified container for multiple Logic handle references, supporting mixed Static and Dynamic lifetimes.
    /// Integrates with <see cref="ProSM{T}"/> and Batcher for scalable, multi-logic execution.
    ///
    /// Features/Configuration:
    /// - Reference Storage: Stores Logic handles as `Ref<Logic<T>>` to avoid expensive struct copies.
    /// - Hybrid Lifecycle: Seamlessly mixes `Static` (permanent) and `Dynamic` (heap-allocated) logic in one collection.
    /// - Slick API: Implicitly converts from `Logic<T>` or `DynamicLogics<T>` via the `LogicGroupBuilder` fluent API.
    /// - Lifecycle Integration: Automatically marks `DynamicLogics<T>` entries as inactive in the group upon their disposal.
    ///
    /// Usage:
    /// - Building: `LogicGroupBuilder<T>.Instance.Add(staticLogic).Add(instanceLogic).Build();`
    /// - Execution: `group.RunAll(ref data);` // Executes all active logics in the group.
    /// - Implicit: `LogicGroup<T> group = myLogic;` // One-line conversion for single-logic groups.
    ///
    /// Validation / Exception Handling:
    /// - Pre-allocated capacity is recommended to ensure pointer stability for internal data.
    /// - `RunAll` uses the `Batcher` to efficiently skip inactive logic entries.
    /// </summary>
    /// <typeparam name="T">Unmanaged data type the logics act upon</typeparam>
    public unsafe struct LogicGroup<T> where T : unmanaged
    {
        public static implicit operator LogicGroup<T>(in Logic<T> logic) => LogicGroupBuilder<T>.Static.Add(logic).Build(out _);
        public static implicit operator LogicGroup<T>(in DynamicLogics<T> logic) => LogicGroupBuilder<T>.Instance.Add(logic).Build(out _);
        
        public Data<Ref<Logic<T>>, MtdLogicGroup> LogicsData;
        public int currentSize => LogicsData.currentSize;
        public int capacity => LogicsData.Capacity;
        public bool isInitialized => LogicsData.DataIsActive;

        // No Auto Collection Allocation
        public LogicGroup(int capacity, out int remainingCapacitySpace) => LogicsData = new Data<Ref<Logic<T>>, MtdLogicGroup>(capacity, Allocator.Persistent, out remainingCapacitySpace);
        
        // Auto Collection Allocation
        public LogicGroup(ReadOnlySpan<(Ref<Logic<T>> logics, bool instanced)> logicGroupComposition, int setCapacity, out int remainingCapacitySpace)
        {
            if(logicGroupComposition.Length > setCapacity) Debug.LogError("Set capacity is smaller than the number of logics in the composition.");
            LogicsData = new Data<Ref<Logic<T>>, MtdLogicGroup>(setCapacity, Allocator.Persistent, out remainingCapacitySpace);

            Debug.Log("[LogicGroup Ctr] Capacity: " + LogicsData.Capacity + "");
            for (int i = 0; i < logicGroupComposition.Length; i++)
            {
                var logicRef = logicGroupComposition[i].logics;
                LogicsData.Allocate(ref logicRef, out _);
                LogicsData.GetWrapper(i).MetaDataVolatile.isDynamic.Set(logicGroupComposition[i].instanced);
                Debug.Log("[LogicGroup Ctr] Current Size: " + LogicsData.currentSize + "");
            }
        }
        
        public LogicGroup(ReadOnlySpan<(RefToStatic<Logic<T>> logics, bool dynamic)> logicGroupComposition, int setCapacity, out int remainingCapacitySpace)
        {
            this.LogicsData = new Data<Ref<Logic<T>>, MtdLogicGroup>(logicGroupComposition.Length + setCapacity, Allocator.Persistent, out remainingCapacitySpace);

            Debug.Log("[LogicGroup Ctr] Capacity: " + LogicsData.Capacity + "");

            for (int i = 0; i < logicGroupComposition.Length; i++)
            {
                var logicRef = logicGroupComposition[i].logics;
                this.LogicsData.Allocate(ref logicRef.AsRefStruct());
                this.LogicsData.GetWrapper(i).MetaDataVolatile.isDynamic.Set(logicGroupComposition[i].dynamic);
            }
        }
        
        
        
        // public void Add(Ref<Logic<T>> logics, out int index)
        // {
        //     Debug.Log("[Before Allocate] currentSize: " + this.LogicsData.currentSize + " capacity: " + this.LogicsData.Capacity + "");
        //     if (this.LogicsData.currentSize >= this.LogicsData.Capacity)
        //         throw new InvalidOperationException("LogicGroup capacity exceeded. Dynamic additions post-initialization are only supported within pre-allocated capacity to ensure pointer stability. " +
        //                                             "If you need to dynamically add logics use the 'additionalCapacity' parameter in LogicGroupBuilder.Dynamic.Build()");
        //
        //     this.LogicsData.Allocate(ref logics, out index);
        //     Debug.Log("Test");
        //     this.LogicsData.GetWrapper(this.LogicsData.currentSize - 1).MetaDataVolatile.isDynamic.Set(true);
        //     Debug.Log("[After Allocate] currentSize: " + this.LogicsData.currentSize + " capacity: " + this.LogicsData.Capacity + "");
        // }
        // public void Add(RefToStatic<Logic<T>> logics)
        // {
        //     Debug.Log("[Before Allocate] currentSize: " + this.LogicsData.currentSize + " capacity: " + this.LogicsData.Capacity + "");
        //     if (this.LogicsData.currentSize >= this.LogicsData.Capacity)
        //         throw new InvalidOperationException("LogicGroup capacity exceeded. Dynamic additions post-initialization are only supported within pre-allocated capacity to ensure pointer stability. " +
        //                                             "If you need to dynamically add logics use the 'additionalCapacity' parameter in LogicGroupBuilder.Dynamic.Build()");
        //
        //     this.LogicsData.Allocate(ref logics.AsRefStruct());
        //     Debug.Log("Test");
        //     this.LogicsData.GetWrapper(this.LogicsData.currentSize - 1).MetaDataVolatile.isDynamic.Set(false);
        //     Debug.Log("[After Allocate] currentSize: " + this.LogicsData.currentSize + " capacity: " + this.LogicsData.Capacity + "");
        // }
        
        

        public void RunAll(ref T data)
        {
            for (int i = 0; i < LogicsData.currentSize; i++)
                if(LogicsData.GetWrapper(i).ActiveElement) 
                    LogicsData[i].GetRef.TryRunAllSequentially(ref data);
            
        }

        public void Dispose()
        {
            LogicsData.Dispose();
        }
    }
    
    public static unsafe class LogicGroupExtensions
    {
        public static ref LogicGroup<T> Add<T>(this ref LogicGroup<T> group, Ref<DynamicLogics<T>> dynLogics) where T : unmanaged
        {
            if (group.LogicsData.currentSize >= group.LogicsData.Capacity)
                throw new InvalidOperationException("LogicGroup capacity exceeded. Dynamic additions post-initialization are only supported within pre-allocated capacity to ensure pointer stability. " +
                                                    "If you need to dynamically add logics use the 'additionalCapacity' parameter in LogicGroupBuilder.Dynamic.Build()");

            group.LogicsData.Allocate(dynLogics.GetRef.RefInstance, out var index);
            
            dynLogics.GetRef.logicGroupIndex = index;
            fixed(void* dataPtr = &group.LogicsData)
            {
                dynLogics.GetRef.parentLogicsDataPtr = dataPtr;
            }

            group.LogicsData.GetWrapper(index).MetaDataVolatile.isDynamic.Set(true);
            return ref group;
        }
        
        public static ref LogicGroup<T> Add<T>(this ref LogicGroup<T> group, RefToStatic<Logic<T>> logics) where T : unmanaged
        {
            if (group.LogicsData.currentSize >= group.LogicsData.Capacity)
                throw new InvalidOperationException("LogicGroup capacity exceeded. Dynamic additions post-initialization are only supported within pre-allocated capacity to ensure pointer stability. " +
                                                    "If you need to dynamically add logics use the 'additionalCapacity' parameter in LogicGroupBuilder.Dynamic.Build()");

            group.LogicsData.Allocate(ref logics.AsRefStruct(), out var index);
            group.LogicsData.GetWrapper(index).MetaDataVolatile.isDynamic.Set(false);
            return ref group;
        }

        
        
    }
}


