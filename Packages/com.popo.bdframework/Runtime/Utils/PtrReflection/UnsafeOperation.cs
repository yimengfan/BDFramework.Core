using System;
using System.Runtime.Serialization;
using MonoVTable = System.IntPtr;

namespace PtrReflection
{
    public unsafe class UnsafeOperation
    {
        public readonly static int PTR_COUNT = sizeof(IntPtr);


        public unsafe static int HeapSizeOf(MonoVTable vtable)//*MonoVTable
        {
            //long* MonoVTable = (long*)*(long*)(*longPtr);
            //long* MonoClass = (long*)*(long*)(*MonoVTable);
            //long element_class = *MonoClass;
            //long cast_class = *(MonoClass + 1);
            //long supertypes = *(MonoClass + 2);
            //byte* kk = (byte*)(MonoClass + 3);
            //UInt16 idepth = *(UInt16*)(kk);
            //kk += 2;

            //byte rank = *(byte*)(kk);
            //kk += 2;

            //int instance_size = *(int*)(kk);

            //long* _MonoType = *(long**)typeof(MyClass2).TypeHandle.Value;
            //long* klass = (long*)*(long*)(*_MonoType);

            //Debug.Log("MonoClass : " + (long)(MonoClass));
            //Debug.Log("klass : " + (long)(klass));

            //Debug.Log("*element_class : " + (element_class));
            //Debug.Log("*cast_class : " + (cast_class));
            //Debug.Log("**supertypes : " + (supertypes));
            //Debug.Log("idepth : " + (idepth));
            //Debug.Log("rank : " + (rank));
            //Debug.Log("instance_size : " + (instance_size));

            IntPtr**** p1 = (IntPtr****)vtable;
            IntPtr* p2 = ***p1;
            p2 += 3;
            int* intPtrIdepth = (int*)p2;
            ++intPtrIdepth;
            int instanceSize = *intPtrIdepth;
            return instanceSize;
        }

        public unsafe static bool IsCreate(Type type)
        {
            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return false;
            }
            return true;
        }

        public unsafe static IntPtr GetTypeHead(Type type)
        {
            object obj = FormatterServices.GetUninitializedObject(type);

#if Use_Unsafe_Tool
            void* ptr = UnsafeTool.unsafeTool.ObjectToVoidPtr(obj);
#else

            ulong gcHandle;
            void* ptr = UnsafeUtility.PinGCObjectAndGetAddress(obj, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);
#endif

            return *(IntPtr*)ptr;
        }


    }
}
