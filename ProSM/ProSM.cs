using ProArchitecture.Data;
using UnityEngine;

namespace ProSM
{
    
    /// <summary>
    /// Instance Defined Procedural State Machine Handle
    /// Layer enums to be declared by the user
    ///
    /// Features/Configuration:
    /// - Any Transitions are evaluated first, followed by direct transitions
    /// - Self Transitions are explicitly ignored (skipped)
    /// - Disposal is idempotent and safe to call multiple times
    /// - Blittable and unmanaged compatible for high performance
    /// - Multi-layered support for parallel state logic
    ///
    /// Usage:
    /// - Initialize() with the number of layers
    /// - InitLayer() for each layer with the default state
    /// - Add_AnyTransition() for any transitions
    /// - Add_DirectTransition() for direct transitions
    /// - AddUpdateLogic(), AddFixedUpdateLogic(), AddLateUpdateLogic() for state-specific tick logic
    /// - AddEnterLogic(), AddExitLogic() for state entry and exit logic
    /// - Entry() to start the FSM
    /// - TickUpdate(), TickFixedUpdate(), TickLateUpdate() to call tick logic
    /// - TryPollTransitions() to check for transitions and update the FSM
    /// - Dispose() to clean up resources
    ///
    /// Validation / Exception Handling:
    /// - Throws ArgumentException if TData is not a blittable type
    /// - Throws InvalidOperationException if Entry() is called without initialized layers
    /// - Throws InvalidOperationException if an allocated layer was never set up via InitLayer()
    /// - Throws InvalidOperationException if TryPollTransitions() is called before Entry() (Unity checks only)
    /// - Throws ArgumentException if enum types do not match the layer's initialized type
    /// - Throws ArgumentOutOfRangeException if state indices are invalid for the layer
    /// - Throws InvalidOperationException if Initialize() is called on an already active FSM
    /// </summary>
    /// <typeparam name="TData">Unmanaged data structure passed through all state logic</typeparam>
    public struct ProSM<TData> where TData : unmanaged
    {
        // make internal later (public rn for testing)
        public Data<LayerData<TData>, LayerMetaData> layers;
        
        public void Dispose()
        {
            if (!layers.DataIsActive)
            {
                Debug.LogWarning("(IDEMPOTENT ACTION) ProSM Dispose called, but ProSM was not initialized or was already disposed. No action taken.");
                return;
            }
            
            for (int i = 0; i < layers.currentSize; i++)
            {
                for(int z = 0; z < layers[i].states.currentSize; z++)
                    layers[i].states[z].Dispose();
                layers[i].states.Dispose();
                layers[i].anyTransitions.Dispose();
            }
            layers.Dispose();
        }
    }
}
