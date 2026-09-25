using System;
using UnityEngine;
using ProArchitecture;
using ProArchitecture.Data;
using ProSM;
using Sirenix.OdinInspector;
using static ProArchitecture.Data.BlittableReference<UnityEngine.CharacterController>;


[Serializable]
public struct PlayerConfig
{
    public float moveSpeed;
    public float rotationSpeed;
}

[Serializable]
public struct PlayerData
{
    public PlayerConfig config;
    
    [ReadOnly] public Vector2 walkInput;
    [ReadOnly] public ByteBool interactionInput;
    [ReadOnly] public BlittableReference<CharacterController> controller;
}

public enum PlayerStates { Idle, Walk, }

public class TopDownPlayer : MonoBehaviour
{
    public PlayerData data;
    ProSM<PlayerData> sm;
    
    public CharacterController controller;
    

    public void Awake()
    {
        sm.Initialize(1);
        sm.InitLayer(0, PlayerStates.Idle);
        
        sm.FixedUpdate(0, PlayerStates.Walk) = PlayerLogic.MoveLogic;
        
        sm.AddDirectTransition(0, PlayerStates.Idle, PlayerStates.Walk, PlayerPredicates.IsMoveInput);
        sm.AddDirectTransition(0, PlayerStates.Walk, PlayerStates.Idle, PlayerPredicates.IsNotMoveInput);
        
        data.controller = Allocate(controller);
    }

    void Start() => sm.Entry(ref data);
    void Update() => sm.TryPollTransitions(ref data);
    void FixedUpdate() => sm.TickFixedUpdate(ref data);
}