using EMILtools.Extensions;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using UnityEngine;

public static unsafe class PlayerPredicates
{
    public static Predicate IsMoveInput = new(&isMoveInput);
    public static Predicate IsNotMoveInput = new(&isMoveInput, true);
    static bool isMoveInput(void* d)
    {
        var data = PtrEX.VoidPtrAsRef<PlayerData>(d);
        return data.walkInput.sqrMagnitude > 0.01f;
    }

}


public static unsafe class PlayerOperations
{
    public static Operation<PlayerData> MoveOp = new(&Move);
    static void Move(PlayerData* d)
    {    
        ref var data = ref PtrEX.AsRef(d);
        Vector2 move = data.walkInput.normalized;
        //Logwin.Log("Player", $"Move: {data.walkInput} | Speed: {data.config.moveSpeed}");
        data.controller.Target.Move(new Vector3(move.x, 0f, move.y) * (data.config.moveSpeed * Time.deltaTime));
    }
    
    public static Operation<PlayerData> LookInMoveDirectionOp = new(&LookInMoveDirection);
    static void LookInMoveDirection(PlayerData* d)
    {
        ref var data = ref PtrEX.AsRef(d);

        Vector2 move = data.walkInput;
        if (move.sqrMagnitude < 0.001f)
            return;

        Vector3 direction = new(move.x, 0f, move.y);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        data.controller.Target.transform.rotation = Quaternion.Slerp(
            data.controller.Target.transform.rotation,
            targetRotation,
            data.config.rotationSpeed * Time.deltaTime);
    }
}


public static class PlayerLogic
{
    public static Logic<PlayerData> MoveLogic = LogicBuilder<PlayerData>.Static
        .Add(PlayerOperations.MoveOp)
        .Add(PlayerOperations.LookInMoveDirectionOp)
        .Build();
    
    
}