using System;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using Unity.Collections;
using UnityEngine;

namespace ProArchitecture.Predicates
{

    public static unsafe class PredicateExpressionBuilder
    {
        public static InstanceBuilder Instance = new InstanceBuilder();
        
        public class InstanceBuilder
        {
            NativeList<PredicateNode> buffer;
            NativeList<PredicateNode> Buffer
            {
                get
                {
                    if(!buffer.IsCreated) buffer = new NativeList<PredicateNode>(8, Allocator.Persistent);
                    return buffer;
                }
            }
            
            public InstanceBuilder Start(RefToStatic<Predicate> predicate)
            {
                var node = new PredicateNode(PredicateType.ROOT, predicate);
                Buffer.Add(node);
                return this;
            }

            public InstanceBuilder And(RefToStatic<Predicate> predicate)
            {
                var node = new PredicateNode(PredicateType.And, predicate);
                Buffer.Add(node);
                return this;
            }

            public InstanceBuilder Or(RefToStatic<Predicate> predicate)
            {
                var node = new PredicateNode(PredicateType.Or, predicate);
                Buffer.Add(node);
                return this;           
            }

            public PredicateExpression Build()
            {
                if(Buffer.IsEmpty) throw new System.Exception("PredicateExpressionBuilder: No predicates added");
                if(Buffer[0].type != PredicateType.ROOT) throw new System.Exception("PredicateExpressionBuilder: First added predicate must be root");
                
                var expression = new PredicateExpression(Buffer.AsReadOnlySpan());
                buffer.Clear();
                return expression;
            }
        }
    }
    
    
    public enum PredicateType { And, Or, ROOT }

    
    public unsafe struct PredicateNode
    {
        public PredicateType type;
        public RefToStatic<Predicate> predicate;
        
        public PredicateNode(PredicateType _type, RefToStatic<Predicate> _predicate)
        {
            type = _type;
            predicate = _predicate;
        }
    }
    
    /// <summary>
    /// Linear logical chain of predicates for complex decision making.
    /// Evaluates multiple predicates sequentially using Left-to-Right precedence.
    ///
    /// Features/Configuration:
    /// - Flat Memory: Utilizes a contiguous `UnsafeList` (via `Data<T>`) for optimal cache locality.
    /// - Shared Lifecycle: Copying the expression struct maintains a handle to the same unmanaged memory.
    /// - Slick API: Provides a fluent builder interface (`And()`, `Or()`) that masks pointer complexity.
    /// - High Performance: Evaluates logic chains at near-native speeds with zero GC allocations.
    ///
    /// Usage:
    /// - Fluent Building: var expr = pA.And(pB).Or(pC);
    /// - Manual Init: var expr = new PredicateExpression(capacity, rootPredicate);
    /// - Evaluation: bool finalResult = expr.Evaluate(ref myData);
    /// - Cleanup: MUST call `Dispose()` once per lifecycle to free unmanaged list memory.
    ///
    /// Validation / Exception Handling:
    /// - Evaluation includes safety checks for disposed or uninitialized expressions.
    /// - Automatically registers with `DataActiveRegistry` for domain-reload safety.
    /// </summary>
    public struct PredicateExpression
    {
        public static implicit operator RefToStatic<PredicateExpression>(in PredicateExpression predicate) => Implicit.Convert(predicate).ToStaticRef();

        internal Data<PredicateNode, NoMtd> nodes;
        
        public PredicateExpression(ReadOnlySpan<PredicateNode> bufferNodes)
        {
            nodes = new Data<PredicateNode, NoMtd>(bufferNodes, Allocator.Persistent);
        }

        public PredicateExpression(int _size, RefToStatic<Predicate> headPredicate)
        {
            var node = new PredicateNode(PredicateType.ROOT, headPredicate);
            nodes = new Data<PredicateNode, NoMtd>(_size, Allocator.Persistent, out _);
            nodes.Allocate(ref node, out int _);
        }

        public PredicateExpression() { }
        
        public int NodeCount => nodes.currentSize;
        public void Dispose() => nodes.Dispose();
    }

    public static unsafe partial class PredicateExtensions
    {
        public static bool EvaluateIdempotent<T>(this ref PredicateExpression expr, ref T Data) 
            where T: unmanaged
        {
            if ((expr.nodes.currentSize == 0 || !expr.nodes.DataIsActive)) return true;
            return expr.Evaluate(ref Data);
        }

        public static bool Evaluate<T>(this ref PredicateExpression expr, ref T Data) 
            where T: unmanaged
        {
            if (expr.nodes.currentSize == 0)
            {
                Debug.LogError("PredicateExpression.Evaluate: no predicates");
                return false;
            }
            if (!expr.nodes.DataIsActive)
            {
                Debug.LogError("PredicateExpression.Evaluate: expression is not active, likely disposed");
                return false;
            }
            
            ref var head = ref expr.nodes[0].predicate.AsRefStatic;
            bool result = head.Evaluate(ref Data);

            for (int i = 1; i < expr.nodes.currentSize; i++)
            {
                ref var p = ref expr.nodes[i].predicate.AsRefStatic;
                switch (expr.nodes[i].type)
                {
                    case PredicateType.And:
                        result = result && p.Evaluate(ref Data);
                        break;

                    case PredicateType.Or:
                        result = result || p.Evaluate(ref Data);
                        break;
                }
            }
            return result;
        }
        
        public static ref PredicateExpression And(this ref PredicateExpression expression, RefToStatic<Predicate> predicate)
        {
            var node = new PredicateNode(PredicateType.And, predicate);
            expression.nodes.Allocate(ref node, out int _);
            return ref expression;
        }
        public static ref PredicateExpression Or(this ref PredicateExpression expression, RefToStatic<Predicate> predicate)
        {
            var ptr = predicate.AsPtrStatic;
            var node = new PredicateNode(PredicateType.Or, predicate);
            expression.nodes.Allocate(ref node, out int _);
            return ref expression;
        }
    }
    
}

