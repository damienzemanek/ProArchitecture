using System;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using Unity.Collections;

namespace ProSM
{
    
    public struct StateData<TData> where TData : unmanaged
    {
        const int TRANSITIONS_SIZE = 50;
        
        public Data<Transition, ProTimersProSM_TransitionMtd> transitions;
        
        public LogicGroup<TData> OnUpdate;
        public LogicGroup<TData> OnFixedUpdate;
        public LogicGroup<TData> OnLateUpdate;
    
        public LogicGroup<TData> OnEnterState;
        public LogicGroup<TData> OnExitState;
        
        public StateData()
        {
            transitions = new Data<Transition, ProTimersProSM_TransitionMtd>(TRANSITIONS_SIZE, Allocator.Persistent, out _);
            
            OnUpdate = default;
            OnFixedUpdate = default;
            OnLateUpdate = default;
            OnEnterState = default;
            OnExitState = default;
        }

        public void Dispose()
        {
            for (int i = 0; i < transitions.currentSize; i++)
                transitions[i].condition.Dispose();
            transitions.Dispose(); OnUpdate.Dispose(); OnFixedUpdate.Dispose(); OnLateUpdate.Dispose(); OnEnterState.Dispose(); OnExitState.Dispose();
        }
    }

    

    public struct Transition
    {
        public short to;
        public PredicateExpression condition;
        public ByteBool hasDurationCondition;
        public ByteBool durationMet;
        
        public int weight;
        
        // mabye in the future make this a logic that does not have to pass in the predicate, but creates it here
        public Transition(short _to, ref PredicateExpression _condition, bool hasDuration, int _weight = 1)
        {
            to = _to;
            condition = _condition;
            hasDurationCondition = new ByteBool(hasDuration);
            durationMet = new ByteBool(false);
            weight = _weight;
        }
    }
    
    public struct ProTimersProSM_TransitionMtd
    {
        public int timerStackRemovalIndex;
        public int layer;
        public IntPtr dataFetchLocationOnComplete;
        public IntPtr fsm;
        
        public ProTimersProSM_TransitionMtd() 
        {
            timerStackRemovalIndex = -1;
            layer = -1;
            dataFetchLocationOnComplete = IntPtr.Zero;
            fsm = IntPtr.Zero;
        }
    }

    
    // Separate Data Layer
    public struct LayerData<TData> where TData : unmanaged // Just the Layer data
    {
        const int ANY_TRANSITIONS_SIZE = 50;
        const int NOT_ENTERED_YET = -1;
        
        public int entryState;
        public int currentState;
        public int previousState;
        
        public Data<StateData<TData>, NoMtd> states;
        public Data<Transition, ProTimersProSM_TransitionMtd> anyTransitions;

        public float timeInState;

        public void Init(int statesSize, int _entryState = 0)
        {
            states = new Data<StateData<TData>, NoMtd>(statesSize, Allocator.Persistent, out _);
            anyTransitions = new Data<Transition, ProTimersProSM_TransitionMtd>(ANY_TRANSITIONS_SIZE, Allocator.Persistent, out _);
            entryState = _entryState;
            currentState = NOT_ENTERED_YET; // Call Entry() to set this
            timeInState = 0;
        }
    }
    public struct LayerMetaData
    {
        public byte isInitialized;
        public bool IsInitialized => isInitialized != 0;
        public long enumTypeId; // hash of enum

        public void Init(long _enumTypeId)
        {
            isInitialized = 1;
            enumTypeId = _enumTypeId;
        }
    }
    
}

    

