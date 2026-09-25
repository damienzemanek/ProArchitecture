using System;
using System.Runtime.CompilerServices;
using ProArchitecture.Predicates;

namespace ProArchitecture.Data
{
    public unsafe struct Ref<T> where T : unmanaged
    {
        T* ptr;
        public ref T GetRef => ref *ptr;
        public T* Ptr => ptr;
        public Ref(ref T value) => ptr = (T*)Unsafe.AsPointer(ref value);
        public Ref(T* pointer) => ptr = pointer;
        public Ref(IntPtr inptr) => ptr = (T*)inptr;
    }

    public unsafe struct ImplicitConverter<T> where T : unmanaged
    {
        public static implicit operator ImplicitConverter<T>(in T value) => new(ref Unsafe.AsRef(value));
        public T* stablePtr;
        public ImplicitConverter(ref T value) => stablePtr = (T*)Unsafe.AsPointer(ref value);
    }

    public static class Implicit
    {
        public static ImplicitConverter<T> Convert<T>(in T value) where T : unmanaged => new(ref Unsafe.AsRef(value));
    }
    
    public static unsafe class RefToStaticExtensions
    {
        public static RefToStatic<T> ToStaticRef<T>(this in ImplicitConverter<T> value) where T : unmanaged => new(value.stablePtr);
        public static ref Ref<T> AsRefStruct<T>(this ref RefToStatic<T> value) where T : unmanaged => ref value.Ref;
    }
    public unsafe struct RefToStatic<T> where T : unmanaged 
    {
        public static implicit operator IntPtr(in RefToStatic<T> value) => (IntPtr)value.Ref.Ptr;
        internal Ref<T> Ref;
        public ref T AsRefStatic => ref Ref.GetRef;
        public T* AsPtrStatic => Ref.Ptr;
        public RefToStatic(ref T value) => Ref = new Ref<T>(ref value);
        public RefToStatic(T* pointer) => Ref = new Ref<T>(pointer);
    }


    // public static class Progress
    // {
    //     // Fails because "The first parameter of the 'in' extension method must be a value type"
    //     // Compiler cannot infer T is a value type
    //     public static RefToStatic<T> ToStaticRef1<T>(this in T value) where T : unmanaged => new(ref Unsafe.AsRef(value));
    //     
    //     
    //     // Works, But is not usable outside of only extending Predicate
    //     // So a Fail
    //     public static RefToStatic<Predicate> ToStaticRef2<T>(this in Predicate value) where T : unmanaged => new(ref Unsafe.AsRef(value));
    //     
    //     
    //     // Solution!
    //     // Works for any T, but requires a small wrapper (SOUNDS GOOD!)
    //     public static unsafe RefToStatic<T> ToStaticRef3<T>(this in TWrapper<T> value) where T : unmanaged => new(value.ptr);
    //     public unsafe struct TWrapper<T> where T : unmanaged
    //     {
    //         public ref T Ref => ref *ptr;
    //         public T* ptr;
    //     }
    // }
}
