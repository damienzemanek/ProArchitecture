using System;
using System.Runtime.CompilerServices;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace ProArchitecture.Predicates
{
    

    /// <summary>
    /// Atomic logic gate for high-performance data evaluation.
    /// Wraps a native function pointer to execute logic directly on unmanaged memory.
    ///
    /// Features/Configuration:
    /// - Zero-Allocation: Evaluates logic via direct function pointers (delegate*) with zero GC overhead.
    /// - Logic Negation: Built-in negation flag allows reusing existing logic functions for inverse cases.
    /// - Slick API: Chaining extensions (`.And()`, `.Or()`) allow for intuitive building of complex expressions.
    /// - Implicit Conversion: Automatically converts to `RefToStatic<Predicate>` for safe expression capture.
    ///
    /// Usage:
    /// - Basic: var isZero = new Predicate(&isZeroFunc);
    /// - Negated: var isNotZero = new Predicate(&isZeroFunc, negate: true);
    /// - Evaluated: bool result = myPredicate.Evaluate(ref myData);
    /// - Fluent API: pA.And(pB).Or(pC); // Returns a PredicateExpression.
    ///
    /// Validation / Exception Handling:
    /// - Evaluate() uses 'fixed' pointers for data stability.
    /// - Unity-specific null checks ensure predicates are initialized before evaluation.
    /// </summary>
    
    public unsafe struct Predicate
    {
        public static implicit operator RefToStatic<Predicate>(in Predicate predicate) => Implicit.Convert(predicate).ToStaticRef();
        public static implicit operator PredicateExpression(in Predicate predicate) => new(1, predicate);

        
        // 1st: void* is the delegate
        // 2nd: bool is the result
        public readonly ByteBool negate;
        internal readonly delegate*<void*, bool> evaluate;
        
        public Predicate(delegate*<void*, bool> _evaluate, bool _negate = false)
        {
            evaluate = _evaluate;
            negate = new ByteBool(_negate);
        }
        
    }
    
    
    
    
    
    
    
    

    public static unsafe partial class PredicateExtensions
    {
        /// <summary>
        /// This override is for simple expressions with only 2 conditions,
        /// If you need more manually call "new PredicateExpression"
        /// </summary>
        /// <param name="head"></param>
        /// <param name="and"></param>
        /// <returns></returns>
        public static PredicateExpression And(this in Predicate head, RefToStatic<Predicate> and)
        {
            var expr = new PredicateExpression(2, head);            
            expr.And(and);
            return expr;
        }

        public static PredicateExpression And(this in Predicate head, RefToStatic<Predicate> and1, RefToStatic<Predicate> and2)
        {
            var expr = new PredicateExpression(3, head);            
            expr.And(and1);
            expr.And(and2);
            return expr;
        }
        
        /// <summary>
        /// This overide is for simple expressions with only 2 conditions
        /// If you need more manually call "new PredicateExpression"
        /// </summary>
        /// <param name="head"></param>
        /// <param name="or"></param>
        /// <returns></returns>
        public static PredicateExpression Or(this ref Predicate head, RefToStatic<Predicate> or)
        {
            var expr = new PredicateExpression(2, head);            
            expr.Or(or);
            return expr;
        }
        
        public static PredicateExpression Or(this ref Predicate head, RefToStatic<Predicate> or1, RefToStatic<Predicate> or2)
        {
            var expr = new PredicateExpression(3, head);            
            expr.Or(or1);
            expr.Or(or2);
            return expr;
        }
        

        public static bool Evaluate<T>(this ref Predicate predicate, ref T data) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (predicate.evaluate == null) throw new NullReferenceException("Predicate not initialized");
#endif
            fixed (void* ptr = &data)
            {
                bool result = predicate.evaluate(ptr);
                return predicate.negate.active ? !result : result;
            }
        }
        
        
    }
    
    




}