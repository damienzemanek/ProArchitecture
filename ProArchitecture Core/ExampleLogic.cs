
///notes:
/// pointer validity depends on where memory lives and how runtime defines safety, not just whether it compiles
/// static CTOR happens AFTER field init. (weird)
/// Static fields init top-to-bottom before static constructuer body runs


using UnityEngine;

namespace ProArchitecture.Logic
{
    public static unsafe class ExampleLogic 
    {
        // 1 BLITTABLE DATA
        // - Cache friendly data that exerts no gc pressure
        public struct ExampleData
        {
            public float x;
        }

        public struct ExampleMetaData
        {
            public float y;
        }
        
        // 2 CONCRETE IMPLEMENTATIONS
        // - Implement your `Operations` statically, auto-validated using `ShouldRun`
        static void Run(ExampleData* data)
        {
            Debug.Log("Running ExampleLogic Operation");
            data->x += 1f;
        }

        static bool ShouldRun(ExampleData* data)
        {
            bool ret = data->x > 1;
            Debug.Log("Checking ExampleLogic Operation : " + ret);
            return ret;
        }
        
        // 3 CONCRETE OPERATION
        // - Compose `Logics` with your implementation using `Operations`
        public static Operation<ExampleData> Operation = new(&Run, &ShouldRun);
        
        // 4 LOGICS CONTAINING OPERATION(S)
        // - Add `Logics` to your ProSM, composed of `LogicOperation`s
        // (!) Not all `Logics` classes will have an OperationLogics handle (!)
        //   - Logics will be composed of many different operations from seperate static Logic classes
        // (!) `Logics` can be instanced to create multiple variations, they do not need to be static, but it is preferred
        public static Logic<ExampleData> OperationLogic = LogicBuilder<ExampleData>.Static.Add( Operation).Build();
    }
}



