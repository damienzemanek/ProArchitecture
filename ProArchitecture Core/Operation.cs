using ProArchitecture.Data;

namespace ProArchitecture.Logic
{
    public static unsafe class OperationExtensions<T> where T : unmanaged
    {
        public static bool AlwaysRuns(T* data) => true; 
        public static Operation<T> EmptyOp = new (&EmptyRun);
        static void EmptyRun(T* data) { }
    }

    /// <summary>
    /// Atomic unit of behavior representing a single function call with conditional execution.
    /// Combines a run delegatde and a should-run predicate into a high-performance unmanaged handle.
    ///
    /// Features/Configuration:
    /// - Blittable & Unmanaged: Optimized for zero-allocation execution via raw function pointers (delegate*).
    /// - Conditional Execution: Built-in 'shouldRun' check integrated directly into the unmanaged call-site.
    /// - Slick API: Supports implicit conversion to RefToStatic, allowing standard "Add()" syntax in builders.
    /// - Memory Safety: Uses 'fixed' pointer blocks to bridge managed 'ref T' to unmanaged 'T*' calls.
    ///
    /// Usage:
    /// - Creation: var op = new Operation<T>(&MyRunFunc, &MyConditionFunc);
    /// - Evaluation: if (op.ShouldRun(in data)) op.Run(ref data);
    /// - Fluid Builder: logicBuilder.Add(myOp); // Implicitly handles static reference capture.
    ///
    /// Validation / Exception Handling:
    /// - Run() and ShouldRun() use 'fixed' statements to ensure data stability during execution.
    /// - Default constructor uses 'AlwaysRuns' predicate to ensure safe execution by default.
    /// </summary>
    /// <typeparam name="T">Unmanaged data type the operation acts upon</typeparam>
    public readonly unsafe struct Operation<T> where T : unmanaged
    {
        public static implicit operator RefToStatic<Operation<T>>(in Operation<T> operation) => Implicit.Convert(operation).ToStaticRef();
        
        readonly delegate*<T*, void> run;
        readonly delegate*<T*, bool> shouldRun;


        public Operation(delegate*<T*, void> _run)
        {
            run = _run;
            shouldRun = &OperationExtensions<T>.AlwaysRuns;
        }


        public Operation(delegate*<T*, void> _run, delegate*<T*, bool> _shouldRun)
        {
            run = _run;
            shouldRun = _shouldRun;
        }

        /// <summary>
        /// converts the ref T to a pointer, and calls the function
        /// </summary>
        /// <param name="data"></param>
        public void Run(ref T data)
        {
            fixed (T* ptr = &data) run(ptr);
        }


        /// <summary>
        /// converst the in T to a pointer, and calls the function
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ShouldRun(in T data)
        {
            fixed (T* ptr = &data) return shouldRun(ptr);
        }
    }
}