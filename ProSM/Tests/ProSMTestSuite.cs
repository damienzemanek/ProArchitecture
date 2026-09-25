using System;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using NUnit.Framework;
using ProArchitecture.Data;
using ProSM;
using UnityEngine;
using static ProArchitecture.Logic.ExampleLogic;

public class ProSMTestSuite : MonoBehaviour
{
    public static unsafe class ExamplePredicates
    {   
        // Little verbose, but oh well
        public static Predicate IsGreaterThanOne = new(&isGreaterThanOne);
        static bool isGreaterThanOne(void* ptr)
        {
            ExampleData* data = (ExampleData*)ptr;
            return data->x > 1;
        }
    }
    
    public enum TestLayerOne { L1S1, L1S2, L1S3 }
    public enum TestLayerTwo { L2S1, L2S2, L2S3 }
    
    [Test]
    public void Test1_Initalizes()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        var exampleData = new ExampleData() { x = 2 };

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);

        Assert.AreEqual(2, exampleData.x);
        Assert.IsTrue(fsm.layers.DataIsActive);
        Assert.IsTrue(fsm.layers.GetWrapper(0).MetaDataVolatile.IsInitialized);
        Assert.IsTrue(fsm.layers.GetWrapper(1).MetaDataVolatile.IsInitialized);
        Assert.AreEqual(3, fsm.layers[0].states.currentSize);
        Assert.AreEqual(3, fsm.layers[1].states.currentSize);
        
        Assert.IsTrue(fsm.layers[0].states[0].transitions.DataIsActive);
        Assert.IsTrue(fsm.layers[1].states[0].transitions.DataIsActive);
        
        fsm.Dispose();
    }
    
    [Test]
    public void Test2_AnyTransitionsInitialize()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);
        fsm.AddAnyTransition(0, TestLayerOne.L1S1, ExamplePredicates.IsGreaterThanOne);
        
        Assert.IsTrue(fsm.layers[0].anyTransitions.DataIsActive);
        Assert.AreEqual(1, fsm.layers[0].anyTransitions.currentSize);

        fsm.Dispose();
    }
    
    [Test]
    public void Test3_DirectTransitionsInitialize()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        
        Assert.IsTrue(fsm.layers[0].states[0].transitions.DataIsActive);
        Assert.AreEqual(1, fsm.layers[0].states[0].transitions.currentSize);

        fsm.Dispose();
    }
    
    [Test]
    public void Test4_EntersIntoCorrectState()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer(0, TestLayerOne.L1S2);
        fsm.InitLayer(1, TestLayerTwo.L2S3);
        var exampleData = new ExampleData() { x = 2 };
        
        fsm.Entry(ref exampleData);

        Assert.AreEqual(1, fsm.layers[0].currentState);
        Assert.AreEqual(2, fsm.layers[1].currentState);

        fsm.Dispose();
    }

    
    [Test] 
    public void Test5_AnyTransitions_TryPollTransitions_AnyTransition_ReturnsBool()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int _);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int _);

        Assert.IsFalse(resultFalse);
        Assert.IsTrue(resultTrue);

        fsm.Dispose();
        
    }
    
    /// <summary>
    /// Solved Issue here: InitLayer is called with 2 layers to init, however Polling needs to check each layer, however
    /// since we didnt init layer 2, we are polling for a transiton on a layer that DNE
    /// I opted for the solution checking state validitiy in Entry instead of requiring the SM to poll for a valid states
    /// This means I am not being defensive and operating my systems in an always valid state, which is better than being defensive
    /// </summary>
    
    [Test] 
    public void Test6_AnyTransitions_TryPollTransitions_DirectTransition_ReturnsBool()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        fsm.AddDirectTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int _);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int _);
        
        Assert.IsFalse(resultFalse);
        Assert.IsTrue(resultTrue);

        fsm.Dispose();
        
    }
    
    
    
    
    [Test] 
    public void Test7_TryPollTransitions_AnyTransition_Transitions()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int nextState_doesNOTtransition);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int nextState2_doesTransition);

        if(resultTrue) fsm.TransitionOnLayer_CallExitEnter(0, nextState2_doesTransition, ref exampleData1);
        
        Assert.AreEqual(-1, nextState_doesNOTtransition);
        Assert.AreEqual(1, nextState2_doesTransition);
        
        Assert.AreEqual(1, fsm.layers[0].currentState);

        fsm.Dispose();
        
    }
    
    [Test] 
    public void Test8_TryPollTransitions_DirectTransition_Transitions()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        fsm.AddDirectTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int nextState_doesNOTtransition);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int nextState2_doesTransition);

        if(resultTrue) fsm.TransitionOnLayer_CallExitEnter(0, nextState2_doesTransition, ref exampleData1);
        
        Assert.AreEqual(-1, nextState_doesNOTtransition);
        Assert.AreEqual(1, nextState2_doesTransition);
        
        Assert.AreEqual(1, fsm.layers[0].currentState);

        fsm.Dispose();
        
    }
    
    
    
    [Test] 
    public void Test9_StateLogic_EnterExitTransitionEventsExecutes()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var exampleData = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData);

        Test9Logic.Reset();

        fsm.layers[0].states[0].OnExitState = Test9Logic.ExitLogics;
        fsm.layers[0].states[1].OnEnterState = Test9Logic.EnterLogics;
        
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData, out int nextState);

        if(resultTrue) fsm.TransitionOnLayer_CallExitEnter(0, nextState, ref exampleData);
        
        Assert.AreEqual(1, nextState);
        Assert.AreEqual(1, fsm.layers[0].currentState);
        Assert.IsTrue(Test9Logic.Exited, "OnExitState should have executed");
        Assert.IsTrue(Test9Logic.Entered, "OnEnterState should have executed");

        fsm.Dispose();
    }

    public static unsafe class Test9Logic
    {
        public static bool Exited;
        public static bool Entered;

        public static void Reset()
        {
            Exited = false;
            Entered = false;
        }

        static void Exit(ExampleData* data) => Exited = true;
        static void Enter(ExampleData* data) => Entered = true;
        static bool ShouldRun(ExampleData* data) => true;

        public static readonly Operation<ExampleData> ExitOp = new(&Exit, &ShouldRun);
        public static readonly Operation<ExampleData> EnterOp = new(&Enter, &ShouldRun);

        public static readonly Logic<ExampleData> ExitLogics = ExitOp;
        public static readonly Logic<ExampleData> EnterLogics = EnterOp;

    }
    
    
    
    
    [Test] 
    public void Test10_StateLogic_TicksExecute()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
    
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData);
        
        TickExampleLogics.Reset();
    
        fsm.layers[0].states[0].OnUpdate = TickExampleLogics.UpdateLogics;
        fsm.layers[0].states[0].OnFixedUpdate = TickExampleLogics.FixedUpdateLogics;
        fsm.layers[0].states[0].OnLateUpdate = TickExampleLogics.LateUpdateLogics;
    
        // Execute the logics
        fsm.layers[0].states[0].OnUpdate.RunAll(ref exampleData);
        fsm.layers[0].states[0].OnFixedUpdate.RunAll(ref exampleData);
        fsm.layers[0].states[0].OnLateUpdate.RunAll(ref exampleData);
    
        Assert.IsTrue(TickExampleLogics.update, "Update logic should have executed");
        Assert.IsTrue(TickExampleLogics.fixedUpdate, "FixedUpdate logic should have executed");
        Assert.IsTrue(TickExampleLogics.lateUpdate, "LateUpdate logic should have executed");
        
        TickExampleLogics.Reset();

        fsm.TickUpdate(ref exampleData);
        fsm.TickFixedUpdate(ref exampleData);
        fsm.TickLateUpdate(ref exampleData);
        
        Assert.IsTrue(TickExampleLogics.update, "Update logic should have executed");
        Assert.IsTrue(TickExampleLogics.fixedUpdate, "FixedUpdate logic should have executed");
        Assert.IsTrue(TickExampleLogics.lateUpdate, "LateUpdate logic should have executed");

        
        fsm.Dispose();
    }
    
    public static unsafe class TickExampleLogics
    {
        public static bool update;
        public static bool lateUpdate;
        public static bool fixedUpdate;

        public static void Reset()
        {
            update = false;
            lateUpdate = false;
            fixedUpdate = false;
        }
        
        static void FixedUpdate(ExampleData* data) => fixedUpdate = true;
        static void LateUpdate(ExampleData* data) => lateUpdate = true;
        static void Update(ExampleData* data) => update = true;
        static bool ShouldRun(ExampleData* data) => true;
    
        public static readonly Operation<ExampleData> UpdateOp = new(&Update, &ShouldRun);
        public static readonly Operation<ExampleData> LateUpdateOp = new(&LateUpdate, &ShouldRun);
        public static readonly Operation<ExampleData> FixedUpdateOp = new(&FixedUpdate, &ShouldRun);

        public static readonly Logic<ExampleData> UpdateLogics = UpdateOp;
        public static readonly Logic<ExampleData> FixedUpdateLogics = FixedUpdateOp;
        public static readonly Logic<ExampleData> LateUpdateLogics = LateUpdateOp;
    }
    
    
    [Test] 
    public void Test11_PollTransitionsAllLayers()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
    
        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);
    
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);
        
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        fsm.AddAnyTransition(1, TestLayerTwo.L2S2, ExamplePredicates.IsGreaterThanOne);
    
        
        // Should not Transition
        fsm.TryPollTransitions(ref exampleData1);
        
        Debug.Log("CurrentStates: layer1: " + fsm.layers[0].currentState + " layer2: " + fsm.layers[1].currentState + "");
        Assert.AreEqual(0, fsm.layers[0].currentState);
        Assert.AreEqual(0, fsm.layers[1].currentState);
        
        // Should Transition
        fsm.TryPollTransitions(ref exampleData2);
        
        Debug.Log("CurrentStates: layer1: " + fsm.layers[0].currentState + " layer2: " + fsm.layers[1].currentState + "");
        Assert.AreEqual(1, fsm.layers[0].currentState);
        Assert.AreEqual(1, fsm.layers[1].currentState);
        
        fsm.Dispose();
    }
    
    [Test]
    public void Test12_TransitionPriority_AnyOverDirect()
    {
        // Tests that AnyTransitions are evaluated before DirectTransitions
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData = new ExampleData() { x = 2 };
    
        fsm.Entry(ref exampleData);
        
        // Add direct transition to S2
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        // Add any transition to S3
        fsm.AddAnyTransition(0, TestLayerOne.L1S3, ExamplePredicates.IsGreaterThanOne);
    
        fsm.TryPollTransitions(ref exampleData);
    
        // Should be S3 because AnyTransitions are polled first in TryPollTransitionsOnLayer
        Assert.AreEqual((int)TestLayerOne.L1S3, fsm.layers[0].currentState);
        
        fsm.Dispose();
    }
    
    
    [Test]
    public void Test13_DataPipeline_ExitToEnter_IsSequential()
    {
        // Verifies that data mutated in OnExit is correctly seen by OnEnter of the next state (Operated Sequentially)
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var data = new ExampleData() { x = 10 };
        fsm.Entry(ref data);
    
        // S1 Exit: x += 1 (11)
        // S2 Enter: x *= 2 (22)
        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnExitState = PipelineLogic.AddOneLogics;
        fsm.layers[0].states[(int)TestLayerOne.L1S2].OnEnterState = PipelineLogic.DoubleLogics;
    
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        fsm.TryPollTransitions(ref data);
    
        Assert.AreEqual(22f, data.x, "Data should be modified by Exit then Enter in sequence");
        fsm.Dispose();
    }
    
    
    // Helper logic class for the new tests
    public static unsafe class PipelineLogic
    {
        static void AddOne(ExampleData* data) => data->x += 1;
        static void Double(ExampleData* data) => data->x *= 2;
        static bool ShouldRun(ExampleData* data) => true;
    
        public static readonly Operation<ExampleData> AddOneOp = new(&AddOne, &ShouldRun);
        public static readonly Operation<ExampleData> DoubleOp = new(&Double, &ShouldRun);

        public static readonly Logic<ExampleData> AddOneLogics= AddOneOp;
        public static readonly Logic<ExampleData> DoubleLogics= DoubleOp;
    }
    
    
    // Helper class for counting executions
    public static unsafe class TransitionCounter
    {
        public static int ExitCount;
        public static int EnterCount;
    
        public static void Reset() { ExitCount = 0; EnterCount = 0; }
    
        static void OnExit(ExampleData* data) => ExitCount++;
        static void OnEnter(ExampleData* data) => EnterCount++;
        static bool ShouldRun(ExampleData* data) => true;
    
        public static readonly Operation<ExampleData> ExitOp = new(&OnExit, &ShouldRun);
        public static readonly Operation<ExampleData> EnterOp = new(&OnEnter, &ShouldRun);
    
        public static readonly Logic<ExampleData> ExitLogics = ExitOp;
        public static readonly Logic<ExampleData> EnterLogics = EnterOp;
    }
    
    [Test]
    public void Test14_SelfTransition_IsIgnored()
    {
        // Verifies that transitioning to the same state is forbidden and triggers no logic
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var data = new ExampleData() { x = 10 };
        fsm.Entry(ref data);
        
        // Setup counters in logic
        TransitionCounter.Reset();
        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnExitState = TransitionCounter.ExitLogics;
        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnEnterState = TransitionCounter.EnterLogics;
    
        // Add transition S1 -> S1 (Self)
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S1, ExamplePredicates.IsGreaterThanOne);
        
        // Poll transitions
        fsm.TryPollTransitions(ref data);
    
        // Assertions
        Assert.AreEqual((int)TestLayerOne.L1S1, fsm.layers[0].currentState, "State should not have changed");
        Assert.AreEqual(0, TransitionCounter.ExitCount, "OnExit should NOT have run for self-transition");
        Assert.AreEqual(0, TransitionCounter.EnterCount, "OnEnter should NOT have run for self-transition");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test15_Stress_MultipleLayersPerformance()
    {
        // Stress test: 100 layers with transitions on every layer
        const int LAYER_COUNT = 100;
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(LAYER_COUNT);
        
        var data = new ExampleData() { x = 2 };
    
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            fsm.InitLayer<TestLayerOne, ExampleData>(i);
            fsm.AddAnyTransition(i, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        }
    
        fsm.Entry(ref data);
        
        // Measure or just verify correctness across large volume
        fsm.TryPollTransitions(ref data);
    
        for (int i = 0; i < LAYER_COUNT; i++)
        {
            Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[i].currentState, $"Layer {i} failed to transition");
        }
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test16_InvalidEnum_ThrowsException()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        var data = new ExampleData() { x = 2 };
    
        // Init layer 0 with TestLayerOne
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry(ref data);
    
    
        // Attempting to add a transition using TestLayerTwo on a layer initialized with TestLayerOne
        Assert.Throws<ArgumentException>(() => {
            fsm.AddAnyTransition(0, TestLayerTwo.L2S1, ExamplePredicates.IsGreaterThanOne);
        }, "Should throw when adding Any transition with wrong enum type");
    
        Assert.Throws<ArgumentException>(() => {
            fsm.AddDirectTransition(0, TestLayerTwo.L2S1, TestLayerTwo.L2S2, ExamplePredicates.IsGreaterThanOne);
        }, "Should throw when adding Direct transition with wrong enum type");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test17_EntryWithoutLayers_ThrowsException()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        // fsm.Initialize(0); // Or skip initialization entirely
    
        var data = new ExampleData() { x = 0 };
    
        Assert.Throws<InvalidOperationException>(() => {
            fsm.Entry(ref data);
        }, "Should throw if Entry is called on an uninitialized FSM");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test18_DoubleInitialize_Throws()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
    
        Assert.Throws<InvalidOperationException>(() => {
            fsm.Initialize(1);
        }, "Should throw if Initialize is called on an already active FSM");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test19_InvalidLayerCount_Throws()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        
        Assert.Throws<ArgumentException>(() => {
            fsm.Initialize(0);
        }, "Should throw if layerCount is 0");
    
        Assert.Throws<ArgumentException>(() => {
            fsm.Initialize(-1);
        }, "Should throw if layerCount is negative");
    }
    
    [Test]
    public void Test20_InitLayer_OutOfBounds_Throws()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1); // Only index 0 is valid
    
        Assert.Throws<IndexOutOfRangeException>(() => {
            fsm.InitLayer<TestLayerOne, ExampleData>(1);
        }, "Should throw if layerIndex is equal to currentSize");
    
        Assert.Throws<IndexOutOfRangeException>(() => {
            fsm.InitLayer<TestLayerOne, ExampleData>(-1);
        }, "Should throw if layerIndex is negative");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test21_InitLayer_DoubleInit_Throws()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
    
        Assert.Throws<InvalidOperationException>(() => {
            fsm.InitLayer<TestLayerOne, ExampleData>(0);
        }, "Should throw if the same layer index is initialized twice");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test22_AddTransition_InvalidState_Throws()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        // TestLayerOne has 3 states (0, 1, 2). Index 3 is invalid.
        // We cast an int to the Enum to simulate an invalid/out-of-range state
        TestLayerOne invalidState = (TestLayerOne)3;
    
        // Any Transition: Invalid 'to'
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            fsm.AddAnyTransition(0, invalidState, ExamplePredicates.IsGreaterThanOne);
        }, "Should throw if 'to' state index is out of bounds");
    
        // Direct Transition: Invalid 'from'
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            fsm.AddDirectTransition(0, invalidState, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        }, "Should throw if 'from' state index is out of bounds");
    
        // Direct Transition: Invalid 'to'
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            fsm.AddDirectTransition(0, TestLayerOne.L1S1, invalidState, ExamplePredicates.IsGreaterThanOne);
        }, "Should throw if 'to' state index is out of bounds in direct transition");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test23_PollBeforeEntry_Throws()
    {
        // This test only runs if ENABLE_UNITY_COLLECTIONS_CHECKS is defined
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var data = new ExampleData() { x = 2 };
    
        // We specifically avoid calling fsm.Entry(ref data) here
        
        Assert.Throws<InvalidOperationException>(() => {
            fsm.TryPollTransitions(ref data);
        }, "Should throw if TryPollTransitions is called before Entry()");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test24_DoubleDispose_IsSafe()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.Dispose();
    
        // Should not throw
        Assert.DoesNotThrow(() => {
            fsm.Dispose();
        }, "Dispose should be safe to call multiple times or on uninitialized FSMs.");
    }
    
    [Test]
    public void Test25_EntryWithUninitializedLayer_Throws()
    {
        // Setup: Initialize with 2 layers but only configure the first one
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(2);
        
        // Initialize Layer 0
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        // Layer 1 is allocated but NOT initialized via InitLayer()
        
        var exampleData = new ExampleData() { x = 0 };
    
        // Assert: Entry should throw because Layer 1 is uninitialized
        var ex = Assert.Throws<InvalidOperationException>(() => {
            fsm.Entry(ref exampleData);
        });
    
        StringAssert.Contains("Layer 1 has not been initialized", ex.Message);
    
        fsm.Dispose();
    }
    
    // Helper struct for testing non-blittable validation
    public struct NonBlittableData
    {
        public bool someBool; //  the `bool` datatype is unmanaged but NOT blittable in Unity/C#
    }
    
    [Test]
    public void Test26_NonBlittableData_Throws()
    {
        ProSM<NonBlittableData> fsm = new ProSM<NonBlittableData>();
        
        // Should throw ArgumentException because NonBlittableData contains a bool
        Assert.Throws<ArgumentException>(() => {
            fsm.Initialize(1);
        }, "Should throw when initializing with a non-blittable TData type");
    
        fsm.Dispose();
    }
    
    [Test]
    public void Test27_DirectInstanceMutation()
    {
        ProSM<SomeInstance.Data> fsm = new ProSM<SomeInstance.Data>();
    
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SomeInstance.Data>(0);
        var exampleData = new SomeInstance.Data() { x = 2 };
        SomeInstance someInstance = new SomeInstance() { someVariable = exampleData };
        
        fsm.Entry(ref someInstance.someVariable);
        
        fsm.layers[0].states[0].OnUpdate = TickExampleLogicsNaiveImplementation.UpdateLogics;
        fsm.layers[0].states[0].OnFixedUpdate = TickExampleLogicsNaiveImplementation.FixedUpdateLogics;
        fsm.layers[0].states[0].OnLateUpdate = TickExampleLogicsNaiveImplementation.LateUpdateLogics;
    
        fsm.TickUpdate(ref someInstance.someVariable); // 2 + 1(run) = 3
        fsm.TickFixedUpdate(ref someInstance.someVariable); // 3 + 1(run) = 4
        fsm.TickLateUpdate(ref someInstance.someVariable); // 4 + 1(run) = 5
    
        Assert.AreEqual(5f, someInstance.someVariable.x);
        fsm.Dispose();
    }
    
    public class SomeInstance
    {
        public struct Data
        {
            public float x;
        }
        
        public Data someVariable;
    }
    
    /// <summary>
    /// In reality you would really only need one of these so this is sort of overkill having all 3 ticks
    /// </summary>
    public static unsafe class TickExampleLogicsNaiveImplementation
    {
        static void FixedUpdate(SomeInstance.Data* data) 
        {
            data->x += 1;
        }
        static void LateUpdate(SomeInstance.Data* data)
        {
            data->x += 1;
        }
        static void Update(SomeInstance.Data* data)
        {
            data->x += 1;
        }
        
        static bool ShouldRun(SomeInstance.Data* data) => true;
    
        public static readonly Operation<SomeInstance.Data> UpdateOp = new(&Update, &ShouldRun);
        public static readonly Operation<SomeInstance.Data> LateUpdateOp = new(&LateUpdate, &ShouldRun);
        public static readonly Operation<SomeInstance.Data> FixedUpdateOp = new(&FixedUpdate, &ShouldRun);

        public static readonly Logic<SomeInstance.Data> UpdateLogics = UpdateOp;
        public static readonly Logic<SomeInstance.Data> LateUpdateLogics = LateUpdateOp;
        public static readonly Logic<SomeInstance.Data> FixedUpdateLogics = FixedUpdateOp;


    }
    
        [Test]
    public void Test28_TransitionPriority_AnyFails_DirectSucceeds()
    {
        // Verifies that DirectTransitions are evaluated when no AnyTransition matches
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 1 };
        fsm.Entry(ref data);

        // Any transition requires x > 1, so this should fail
        fsm.AddAnyTransition(0, TestLayerOne.L1S3, ExamplePredicates.IsGreaterThanOne);

        // Direct transition always succeeds
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, AlwaysTruePredicate.Instance);

        fsm.TryPollTransitions(ref data);

        Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[0].currentState, "Direct transition should execute when Any transition does not match");
        fsm.Dispose();
    }


    [Test]
    public void Test29_MultipleDirectTransitions_FirstMatchingTransitionWins()
    {
        // Verifies that when multiple DirectTransitions match,
        // the first matching transition is selected
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 2 };
        fsm.Entry(ref data);

        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S3, ExamplePredicates.IsGreaterThanOne);

        fsm.TryPollTransitions(ref data);

        Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[0].currentState, "First matching DirectTransition should win");

        fsm.Dispose();
    }


    [Test]
    public void Test30_MultipleAnyTransitions_FirstMatchingTransitionWins()
    {
        // Verifies that when multiple AnyTransitions match,
        // the first matching transition is selected
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 2 };
        fsm.Entry(ref data);

        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        fsm.AddAnyTransition(0, TestLayerOne.L1S3, ExamplePredicates.IsGreaterThanOne);

        fsm.TryPollTransitions(ref data);

        Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[0].currentState, "First matching AnyTransition should win");

        fsm.Dispose();
    }


    [Test]
    public void Test31_Entry_ExecutesInitialStateEnter()
    {
        // Verifies that Entry executes the OnEnterState logic
        // of the configured initial state
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 10 };

        TransitionCounter.Reset();

        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnEnterState = TransitionCounter.EnterLogics;

        fsm.Entry(ref data);

        Assert.AreEqual(1, TransitionCounter.EnterCount, "Initial state's OnEnterState should have executed");

        fsm.Dispose();
    }


    [Test]
    public void Test32_Entry_OnlyExecutesInitialStateEnter()
    {
        // Verifies that Entry only executes the OnEnterState logic
        // for the state's initial state
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 10 };

        TransitionCounter.Reset();

        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnEnterState = TransitionCounter.EnterLogics;
        fsm.layers[0].states[(int)TestLayerOne.L1S2].OnEnterState = TransitionCounter.EnterLogics;

        fsm.Entry(ref data);
        Assert.AreEqual((int)TestLayerOne.L1S1, fsm.layers[0].currentState);
        Assert.AreEqual(1, TransitionCounter.EnterCount, "Only the initial state's OnEnterState should have executed");

        fsm.Dispose();
    }


    [Test]
    public void Test33_TickExecutesCurrentStateLogic()
    {
        // Verifies that TickUpdate executes the logic belonging
        // to the currently active state
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var data = new ExampleData() { x = 0 };
        
        fsm.Entry(ref data);

        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnUpdate = TickStateLogic.StateOneLogics;
        fsm.layers[0].states[(int)TestLayerOne.L1S2].OnUpdate = TickStateLogic.StateTwoLogics;

        fsm.TickUpdate(ref data);

        // S1 adds 1
        Assert.AreEqual(1f, data.x, "Current state's Update logic should have executed");

        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);

        // Change x so the transition predicate succeeds
        data.x = 2;

        fsm.TryPollTransitions(ref data);

        Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[0].currentState);

        fsm.TickUpdate(ref data);

        // S2 adds 100
        Assert.AreEqual(102f, data.x, "New current state's Update logic should have executed");

        fsm.Dispose();
    }


    [Test]
    public void Test34_Logic_ShouldRunFalse_SkipsOperation()
    {
        // Verifies that an Operation does not execute when ShouldRun returns false
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 10 };

        fsm.Entry(ref data);

        ConditionalLogic.Reset();

        fsm.layers[0].states[0].OnUpdate = ConditionalLogic.ShouldNotRunLogics;

        fsm.TickUpdate(ref data);

        Assert.AreEqual(0, ConditionalLogic.ExecutionCount, "Operation should not execute when ShouldRun returns false");
        Assert.AreEqual(10f, data.x, "Data should not have been modified");

        fsm.Dispose();
    }


    [Test]
    public void Test35_Logic_MultipleOperations_ExecuteSequentially()
    {
        // Verifies that multiple Operations inside a Logic execute sequentially
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 10 };

        fsm.Entry(ref data);

        fsm.layers[0].states[0].OnUpdate = MultipleOperationLogic.Logics;

        fsm.TickUpdate(ref data);

        // 10 + 1 = 11
        // 11 * 2 = 22
        Assert.AreEqual(22f, data.x, "Operations should execute sequentially in the Logic");

        fsm.Dispose();
    }


    [Test]
    public void Test36_PollAfterTransition_EvaluatesNewCurrentState()
    {
        // Verifies that after transitioning into a new state,
        // polling again evaluates transitions belonging to the new state
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 2 };

        fsm.Entry(ref data);

        // S1 -> S2
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ExamplePredicates.IsGreaterThanOne);
        // S2 -> S3
        fsm.AddDirectTransition(0, TestLayerOne.L1S2, TestLayerOne.L1S3, ExamplePredicates.IsGreaterThanOne);

        fsm.TryPollTransitions(ref data);
        Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[0].currentState);

        fsm.TryPollTransitions(ref data);
        Assert.AreEqual((int)TestLayerOne.L1S3, fsm.layers[0].currentState, "Second poll should evaluate transitions belonging to the new state");

        fsm.Dispose();
    }


    [Test]
    public void Test37_DisposedFSM_Entry_Throws()
    {
        // Verifies that public operations cannot be used after the FSM has been disposed
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 2 };

        fsm.Entry(ref data);
        fsm.Dispose();

        Assert.Throws<InvalidOperationException>(() => {
            fsm.Entry(ref data);
        }, "Entry should throw after Dispose");
        
    }
    
    [Test]
    public void Test38_DisposedFSM_RuntimeOperationHandling()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);

        var data = new ExampleData() { x = 2 };

        fsm.Entry(ref data);
        fsm.Dispose();

        // Runtime operations are not explicitly lifecycle-validated.
        // The underlying Data<T> throws when accessed after Dispose.
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            fsm.TryPollTransitions(ref data);
        });

        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            fsm.TickUpdate(ref data);
        });

        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            fsm.TickFixedUpdate(ref data);
        });

        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            fsm.TickLateUpdate(ref data);
        });
    }
    
    public static unsafe class AlwaysTruePredicate
    {
        public static bool Evaluate(void* ptr) => true;
        public static readonly Predicate Instance = new(&Evaluate);
    }


    public static unsafe class TickStateLogic
    {
        static void StateOneUpdate(ExampleData* data)
        {
            data->x += 1;
        }

        static void StateTwoUpdate(ExampleData* data)
        {
            data->x += 100;
        }
        
        public static readonly Operation<ExampleData> StateOneOp = new(&StateOneUpdate);
        public static readonly Operation<ExampleData> StateTwoOp = new(&StateTwoUpdate);

        public static readonly Logic<ExampleData> StateOneLogics = StateOneOp;
        public static readonly Logic<ExampleData> StateTwoLogics = StateTwoOp;
    }


    public static unsafe class ConditionalLogic
    {
        public static int ExecutionCount;

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        static void Execute(ExampleData* data)
        {
            ExecutionCount++;
            data->x += 100;
        }

        static bool ShouldNotRun(ExampleData* data) => false;

        public static readonly Operation<ExampleData> ShouldNotRunOp = new(&Execute, &ShouldNotRun);

        public static readonly Logic<ExampleData> ShouldNotRunLogics = ShouldNotRunOp;
    }


    public static unsafe class MultipleOperationLogic
    {
        static void AddOne(ExampleData* data)
        {
            data->x += 1;
        }

        static void MultiplyByTwo(ExampleData* data)
        {
            data->x *= 2;
        }
        
        public static readonly Operation<ExampleData> AddOneOp = new(&AddOne);

        public static readonly Operation<ExampleData> MultiplyByTwoOp = new(&MultiplyByTwo);

        public static readonly Logic<ExampleData> Logics = LogicBuilder<ExampleData>.Static
                                                            .Add(AddOneOp)
                                                            .Add(MultiplyByTwoOp)
                                                            .Build();
    }
    

    

}
