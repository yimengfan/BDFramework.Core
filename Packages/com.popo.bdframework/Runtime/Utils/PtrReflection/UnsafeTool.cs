using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;

namespace PtrReflection
{
#if Use_Unsafe_Tool

    [StructLayout(LayoutKind.Explicit)]
    public unsafe class UnsafeTool
    {
        public static UnsafeTool unsafeTool = new UnsafeTool();

        public delegate void* ObjectToVoidPtrDelegate(object obj);
        public delegate IntPtr* ObjectToIntPtrDelegate(object obj);
        public delegate byte* ObjectToBytePtrDelegate(object obj);
        public delegate void CopyObjectDelegate(void* ptr, object obj);


        [FieldOffset(0)]
        public ObjectToVoidPtrDelegate ObjectToVoidPtr;
        [FieldOffset(0)]
        public ObjectToIntPtrDelegate ObjectToIntPtr;
        [FieldOffset(0)]
        public ObjectToBytePtrDelegate ObjectToBytePtr;
        [FieldOffset(0)]
        Func<object, object> func;

        public delegate object VoidPtrToObjectDelegate(void* ptr);
        public delegate object IntPtrToObjectDelegate(IntPtr* ptr);
        public delegate object BytePtrToObjectDelegate(byte* ptr);

        [FieldOffset(8)]
        public VoidPtrToObjectDelegate VoidPtrToObject;
        [FieldOffset(8)]
        public IntPtrToObjectDelegate IntPtrToObject;
        [FieldOffset(8)]
        public BytePtrToObjectDelegate BytePtrToObject;
        [FieldOffset(8)]
        Func<object, object> func2;


        [FieldOffset(16)]
        public CopyObjectDelegate SetObject;
        [FieldOffset(16)]
        CopyObjectDelegate_ func3;
        delegate void CopyObjectDelegate_(void** ptr, void* obj);

        public UnsafeTool()
        {
            func = Out;
            func2 = Out;
            func3 = _CopyObject;
        }
        object Out(object o) { return o; }
        void _CopyObject(void** ptr, void* obj) 
        {
            *ptr = obj; 
        }
    }
#else

#endif
}
