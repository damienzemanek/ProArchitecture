using System;
using System.Runtime.CompilerServices;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ProSM
{
    
    public static partial class ProSMLogic
    {
        // Initalization
        public static void Initialize<TData>(this ref ProSM<TData> fsm, int layerCount) 
            where TData : unmanaged
        {
            // Ensure TData is blittable (Input Validation)
            if (!UnsafeUtility.IsBlittable<TData>())
                throw new ArgumentException($"Type '{typeof(TData).Name}' is not blittable. TData must be a blittable type to ensure safety in unmanaged memory operations.");
                
            // Prevent memory leaks (Input Validation)
            if (fsm.layers.DataIsActive) 
                throw new InvalidOperationException("ProSM is already initialized. Dispose it before initializing again.");
        
            // Valid Layer Count (Input Validation)
            if (layerCount <= 0) 
                throw new ArgumentException("layerCount must be greater than 0.");

                
            fsm.layers = new Data<LayerData<TData>, LayerMetaData>(layerCount, Allocator.Persistent, out _);
            // Creating the layer data
            for (int i = 0; i < layerCount; i++)
            {
                var layerData = new LayerData<TData>() { entryState = 0, currentState = 0, previousState = 0 };
                fsm.layers.Allocate(ref layerData, out int id);
            }
        }
            
        public static void InitLayer<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates defaultState = default) 
            where TData : unmanaged
            where TStates : Enum
        {
            // Valid Layer (Input Validation)
            if (layerIndex < 0 || layerIndex >= fsm.layers.currentSize)
                throw new IndexOutOfRangeException($"Layer index {layerIndex} is out of bounds (Size: {fsm.layers.currentSize}).");
                
            ref var layerData = ref fsm.layers.GetWrapper(layerIndex);
                
            // Layer not already initialized (Input Validation)
            if(layerData.MetaDataVolatile.IsInitialized) 
                throw new InvalidOperationException("Layer already initialized, use another index");
                
            layerData.MetaDataVolatile.enumTypeId = typeof(TStates).GetHashCode(); // Store type hash
            var stateCount = Enum.GetValues(typeof(TStates)).Length;

            var entryState = Unsafe.As<TStates, int>(ref defaultState);
            layerData.DataVolatile.Init(stateCount, entryState);
            layerData.MetaDataVolatile.Init(typeof(TStates).GetHashCode());
            for (int i = 0; i < stateCount; i++)
            {
                var stateData = new StateData<TData>() { transitions = new Data<Transition, ProTimersProSM_TransitionMtd>(10, Allocator.Persistent, out _) };
                layerData.DataVolatile.states.Allocate(ref stateData, out int _);
            }
        }
            
            
        // Add Transitions
        public static void AddAnyTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates to, PredicateExpression predicate, int weight = 1)
            where TData : unmanaged
        {
            // Valid Enum (Input Validation)
            if (typeof(TStates).GetHashCode() != fsm.layers.GetWrapper(layerIndex).MetaDataVolatile.enumTypeId)
                throw new ArgumentException($"Enum type '{typeof(TStates).Name}' does not match the type used to initialize layer {layerIndex}.");
                
            // Valid To (Input Validation)
            int toIndex = Unsafe.As<TStates, int>(ref to);
            if (toIndex < 0 || toIndex >= fsm.layers.GetWrapper(layerIndex).DataVolatile.states.currentSize)
                throw new ArgumentOutOfRangeException(nameof(to), $"State {to} (index {toIndex}) does not exist in layer {layerIndex}.");
                
            var transition = new Transition( (short)toIndex, ref predicate, false, weight);
            fsm.layers[layerIndex].anyTransitions.Allocate(ref transition, out int _);
        }

        public static void AddDirectTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates from, TStates to, PredicateExpression predicate, int weight = 1)    
            where TData : unmanaged
        {
            // Valid Enum (Input Validation)
            if (typeof(TStates).GetHashCode() != fsm.layers.GetWrapper(layerIndex).MetaDataVolatile.enumTypeId)
                throw new ArgumentException($"Enum type '{typeof(TStates).Name}' does not match the type used to initialize layer {layerIndex}.");

            // Valid From (Input Validation)
            int fromIndex = Unsafe.As<TStates, int>(ref from);
            if (fromIndex < 0 || fromIndex >= fsm.layers[layerIndex].states.currentSize)
                throw new ArgumentOutOfRangeException(nameof(from), $"State {from} (index {fromIndex}) does not exist in layer {layerIndex}.");

            // Valid To (Input Validation)
            int toIndex = Unsafe.As<TStates, int>(ref to);
            if (toIndex < 0 || toIndex >= fsm.layers[layerIndex].states.currentSize)
                throw new ArgumentOutOfRangeException(nameof(to), $"State {to} (index {toIndex}) does not exist in layer {layerIndex}.");

            var transition = new Transition((short)toIndex, ref predicate, false, weight);
            fsm.layers[layerIndex].states[fromIndex].transitions.Allocate(ref transition, out int _);
        }
        

        public static ref LogicGroup<TData> Update<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates state)
            where TData : unmanaged    where TStates : Enum
        {
            int stateIndex = Unsafe.As<TStates, int>(ref state);
            return ref fsm.layers[layerIndex].states[stateIndex].OnUpdate;
        }

        public static ref LogicGroup<TData> FixedUpdate<TStates, TData>( this ref ProSM<TData> fsm, int layerIndex, TStates state)
            where TData : unmanaged    where TStates : Enum
        {
            int stateIndex = Unsafe.As<TStates, int>(ref state);
            return ref fsm.layers[layerIndex].states[stateIndex].OnFixedUpdate;
        }
        
        public static ref LogicGroup<TData> LateUpdate<TStates, TData>( this ref ProSM<TData> fsm, int layerIndex, TStates state)
            where TData : unmanaged    where TStates : Enum
        {
            int stateIndex = Unsafe.As<TStates, int>(ref state);
            return ref fsm.layers[layerIndex].states[stateIndex].OnLateUpdate;
        }
        
        public static ref LogicGroup<TData> Enter<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates state)
            where TData : unmanaged    where TStates : Enum
        {
            int stateIndex = Unsafe.As<TStates, int>(ref state);
            return ref fsm.layers[layerIndex].states[stateIndex].OnEnterState;
        }
        
        public static ref LogicGroup<TData> Exit<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates state)
            where TData : unmanaged    where TStates : Enum
        {
            int stateIndex = Unsafe.As<TStates, int>(ref state);
            return ref fsm.layers[layerIndex].states[stateIndex].OnExitState;
        }

        public static void TryPollTransitions<TData>(this ref ProSM<TData> fsm, ref TData data)
            where TData : unmanaged
        {
            const int NO_NEW_LAYER_FOUND = -1;

            // Poll each layer -> Transition if found a next state
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
    #if ENABLE_UNITY_COLLECTIONS_CHECKS // Hot path so im using the compiler directive to remove it in builds
                // Sequential input validation: Entry() must be called before polling.
                if (fsm.layers[i].currentState == -1) 
                    throw new InvalidOperationException($"Layer {i} has not been entered. Call Entry() before polling transitions.");
    #endif 
                fsm.TryPollTransitionsOnLayer(ref fsm.layers[i], ref data, out int nextState);
                if (nextState != NO_NEW_LAYER_FOUND) 
                    fsm.TransitionOnLayer_CallExitEnter(i, nextState, ref data);
            }
        }
            
        // public for testing
        public static bool TryPollTransitionsOnLayer<TData>(this ref ProSM<TData> fsm, ref LayerData<TData> layerdata, ref TData data, out int nextState)
            where TData : unmanaged
        {
            const int NO_NEW_LAYER_FOUND = -1;

            // 1. Any Transitions (Priority - First Match)
            for(int i = 0; i < layerdata.anyTransitions.currentSize; i++)
            {
                ref var transition = ref layerdata.anyTransitions[i];
                if (!transition.condition.Evaluate(ref data)) continue;
                if(layerdata.currentState == transition.to) continue;
                nextState = transition.to;
                return true;
            }
                
            // 2. Direct Transitions (Weighted Random)
            ref var currentStateData = ref layerdata.states[layerdata.currentState];
            int totalWeight = 0;

            // First pass: calculate total weight of valid transitions
            for(int i = 0; i < currentStateData.transitions.currentSize; i++)        
            {
                ref var transition = ref currentStateData.transitions[i];
                if (transition.condition.Evaluate(ref data))
                {
                    if (layerdata.currentState == transition.to) continue;
                    totalWeight += transition.weight;
                }
            }

            if (totalWeight > 0)
            {
                int roll = UnityEngine.Random.Range(0, totalWeight);
                int currentWeightSum = 0;

                // Second pass: select based on weight
                for(int i = 0; i < currentStateData.transitions.currentSize; i++)
                {
                    ref var transition = ref currentStateData.transitions[i];
                    if (transition.condition.Evaluate(ref data))
                    {
                        if (layerdata.currentState == transition.to) continue;
                        currentWeightSum += transition.weight;
                        if (roll < currentWeightSum)
                        {
                            nextState = transition.to;
                            return true;
                        }
                    }
                }
            }
                
            nextState = NO_NEW_LAYER_FOUND;
            return false;
        }

        public static void TransitionOnLayer_CallExitEnter<TData>(this ref ProSM<TData> fsm, int layer, int nextState, ref TData data)
            where TData : unmanaged
        {
            // prev
            fsm.layers[layer].previousState = fsm.layers[layer].currentState;
            fsm.layers[layer].states[fsm.layers[layer].previousState].OnExitState.RunAll(ref data);
        
            // next
            fsm.layers[layer].currentState = nextState;
            fsm.layers[layer].states[nextState].OnEnterState.RunAll(ref data);
        
            fsm.layers[layer].timeInState = 0;
        }
            
            
            
        // Entry (Awake)
        public static void Entry<TData>(this ref ProSM<TData> fsm, ref TData data)
            where TData : unmanaged
        {
            // (Sequential Input Validation)
            if (!fsm.layers.DataIsActive || fsm.layers.currentSize == 0)
                throw new InvalidOperationException("ProSM Entry failed: No layers have been initialized. Call Initialize() and InitLayer() first.");
                
                
            for(int i = 0; i < fsm.layers.currentSize; i++)
            {
                ref var layerData = ref fsm.layers.GetWrapper(i);
                if(layerData.MetaDataVolatile.IsInitialized == false) 
                    throw new InvalidOperationException($"ProSM Entry failed: Layer {i} has not been initialized. Call InitLayer() before calling Entry().");
                layerData.DataVolatile.currentState = layerData.DataVolatile.entryState;
                fsm.layers[i].timeInState = 0;
                fsm.layers[i].states[layerData.DataVolatile.currentState].OnEnterState.RunAll(ref data);
                // enter logic using StateLogics
            }
        }
            
        // State Ticks with internal TickLogicData creation
        public static void TickUpdate<TData>(this ref ProSM<TData> fsm, ref TData data) 
            where TData : unmanaged
        {
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                fsm.layers[i].timeInState += Time.deltaTime;
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnUpdate.RunAll(ref data);
            }
        }

        public static void TickFixedUpdate<TData>(this ref ProSM<TData> fsm, ref TData data)
            where TData : unmanaged
        {
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnFixedUpdate.RunAll(ref data);
            }
        }

        public static void TickLateUpdate<TData>(this ref ProSM<TData> fsm, ref TData data)
            where TData : unmanaged
        {
        
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnLateUpdate.RunAll(ref data);
            }
        }
    }
}

