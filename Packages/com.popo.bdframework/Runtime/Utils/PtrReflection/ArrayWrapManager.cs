using System;
using Unity.Collections.LowLevel.Unsafe;

namespace PtrReflection
{
    public unsafe static class ArrayWrapManager
    {
        public static IArrayWrap GetIArrayWrap(Type type)
        {
            var itemType = type.GetElementType();
            int rank = type.GetArrayRank();
            if (rank == 1)
            {
                ArrayWrapRankOne arrayWrapRank = new ArrayWrapRankOne(1, itemType);
                return arrayWrapRank;
            }
            else
            {
                ArrayWrapRank arrayWrapRank = new ArrayWrapRank(rank, itemType);
                return arrayWrapRank;
            }
        }
    }

    public unsafe abstract class IArrayWrap
    {
        /// <summary>
        /// 数组的元素类型的TypeCode
        /// </summary>
        public readonly TypeCode elementTypeCode;
        /// <summary>
        /// 数组的类型头指针
        /// </summary>
        public readonly IntPtr head;
        /// <summary>
        /// 数组的秩
        /// </summary>
        public readonly int rank;
        /// <summary>
        /// 数组的元素长度
        /// </summary>
        public readonly int elementTypeSize;
        /// <summary>
        /// 数组的元素类型
        /// </summary>
        public readonly Type elementType;
        /// <summary>
        /// 数组的元素是否是值类型，否就是引用类型
        /// </summary>
        public readonly bool isValueType;
        /// <summary>
        /// 数组的元素的类型头指针，只有元素类型是引用类型的话，才需要这个
        /// </summary>
        public IntPtr elementTypeHead;

        public IArrayWrap(int rank, Type elementType)
        {
            this.rank = rank;
            this.elementType = elementType;
            this.isValueType = elementType.IsValueType;
            if (isValueType)
            {
                this.elementTypeSize = UnsafeUtility.SizeOf(elementType);
            }
            else
            {
                this.elementTypeSize = UnsafeOperation.PTR_COUNT;
            }
            this.elementTypeCode = Type.GetTypeCode(elementType);
            if (rank == 1)
            {
#if Use_Unsafe_Tool
                this.head = *UnsafeTool.unsafeTool.ObjectToIntPtr(Array.CreateInstance(elementType, 0));
#else
                ulong gcHandle;
                this.head = *(IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(Array.CreateInstance(elementType, 0), out gcHandle);
                UnsafeUtility.ReleaseGCObject(gcHandle);
#endif
            }
            else
            {
#if Use_Unsafe_Tool
                this.head = *UnsafeTool.unsafeTool.ObjectToIntPtr(Array.CreateInstance(elementType, new int[rank]));
#else
                ulong gcHandle;
                this.head = *(IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(Array.CreateInstance(elementType, new int[rank]), out gcHandle);
                UnsafeUtility.ReleaseGCObject(gcHandle);
#endif
            }
        }

        public abstract object CreateArray(ref ArrayWrapOutData arrayWrapData);

        public abstract object CreateArray(int length);

        public abstract ArrayWrapOutData GetArrayData(Array array);

        public unsafe object GetValue(void* source, int index)
        {
            byte* field = (byte*)source;
            field += index * this.elementTypeSize;

            if (this.isValueType)
            {
                switch (elementTypeCode)
                {
                    case TypeCode.Boolean:
                        return *(bool*)field;
                    case TypeCode.Byte:
                        return *(Byte*)field;
                    case TypeCode.Char:
                        return *(Char*)field;
                    case TypeCode.DateTime:
                        return *(DateTime*)field;
                    case TypeCode.Decimal:
                        return *(Decimal*)field;
                    case TypeCode.Double:
                        return *(Double*)field;
                    case TypeCode.Int16:
                        return *(Int16*)field;
                    case TypeCode.Int32:
                        return *(Int32*)field;
                    case TypeCode.Int64:
                        return *(Int64*)field;
                    case TypeCode.SByte:
                        return *(Int64*)field;
                    case TypeCode.Single:
                        return *(Single*)field;
                    case TypeCode.UInt16:
                        return *(UInt16*)field;
                    case TypeCode.UInt32:
                        return *(UInt32*)field;
                    case TypeCode.UInt64:
                        return *(UInt64*)field;
                    case TypeCode.Object:
                    default:
                        //GC.Collect(); 
                        //return null;  
                        //ulong gcHandle;
                        object obj = new byte[this.elementTypeSize - 1 * UnsafeOperation.PTR_COUNT];
                        //IntPtr* ptr = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(obj, out gcHandle);

#if Use_Unsafe_Tool
                        IntPtr* ptr = UnsafeTool.unsafeTool.ObjectToIntPtr(obj);
#else
                        ulong gcHandle;
                        IntPtr* ptr = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(obj, out gcHandle);
                        UnsafeUtility.ReleaseGCObject(gcHandle);
#endif

                        if (elementTypeHead == default(IntPtr))
                        {
                            elementTypeHead = UnsafeOperation.GetTypeHead(elementType);
                        }
                        *ptr = elementTypeHead;
                        ptr += 2;
                        UnsafeUtility.MemCpy(ptr, field, this.elementTypeSize);
                        //UnsafeUtility.ReleaseGCObject(gcHandle);
                        //GC.Collect();
                        return obj;
                }
            }
            else
            {
                return UnsafeUtility.ReadArrayElement<object>(field, 0);
                //return GeneralTool.VoidToObject(*(IntPtr**)field);
            }
        }

        public unsafe void SetValue(void* source, int index, object value)
        {
            byte* field = (byte*)source;
            field += index * this.elementTypeSize;

            if (this.isValueType)
            {
                switch (elementTypeCode)
                {
                    case TypeCode.Boolean:
                        *(bool*)field = (bool)value;
                        break;
                    case TypeCode.Byte:
                        *(Byte*)field = (Byte)value;
                        break;
                    case TypeCode.Char:
                        *(Char*)field = (Char)value;
                        break;
                    case TypeCode.DateTime:
                        *(DateTime*)field = (DateTime)value;
                        break;
                    case TypeCode.Decimal:
                        *(Decimal*)field = (Decimal)value;
                        break;
                    case TypeCode.Double:
                        *(Double*)field = (Double)value;
                        break;
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Int16:
                        *(Int16*)field = (Int16)value;
                        break;
                    case TypeCode.Int32:
                        *(Int32*)field = (Int32)value;
                        break;
                    case TypeCode.Int64:
                        *(Int64*)field = (Int64)value;
                        break;
                    case TypeCode.SByte:
                        *(SByte*)field = (SByte)value;
                        break;
                    case TypeCode.Single:
                        *(Single*)field = (Single)value;
                        break;
                    case TypeCode.UInt16:
                        *(UInt16*)field = (UInt16)value;
                        break;
                    case TypeCode.UInt32:
                        *(UInt32*)field = (UInt32)value;
                        break;
                    case TypeCode.UInt64:
                        *(UInt64*)field = (UInt64)value;
                        break;
                    case TypeCode.String:
                    case TypeCode.Object:

#if Use_Unsafe_Tool
                        IntPtr* ptr = UnsafeTool.unsafeTool.ObjectToIntPtr(value);
#else
                        ulong gcHandle;
                        IntPtr* ptr = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(value, out gcHandle);
                        UnsafeUtility.ReleaseGCObject(gcHandle);
#endif
                        UnsafeUtility.MemCpy(field, ptr + 2, elementTypeSize);
                        break;
                }
            }
            else
            {
                UnsafeUtility.CopyObjectAddressToPtr(value, field);
                //GeneralTool.SetObject(field, value);
            }
        }

    }

    public unsafe struct ArrayWrapOutData
    {
        public int length;
        public int[] arrayLengths;
        public void* objPtr;
        public byte* startItemOffcet;
        //public GCHandle gCHandle;
    }
    unsafe class ArrayWrapRank : IArrayWrap
    {
        public ArrayWrapRank(int rank, Type elementType) : base(rank, elementType) { }

        public override object CreateArray(ref ArrayWrapOutData arrayWrapData)
        {
            var array = Array.CreateInstance(elementType, arrayWrapData.arrayLengths);

            ulong gcHandle;
#if Use_Unsafe_Tool
            IntPtr* p = UnsafeTool.unsafeTool.ObjectToIntPtr(array);
#else
            IntPtr* p = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);
#endif
            arrayWrapData.objPtr = p;
            arrayWrapData.startItemOffcet = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(array, out gcHandle);

            return array;
        }

        public override object CreateArray(int length)
        {
              return null ;
        }

        public override ArrayWrapOutData GetArrayData(Array array) 
        {
            ArrayWrapOutData arrayWrapData = new ArrayWrapOutData();
            arrayWrapData.arrayLengths = new int[array.Rank];
            for (int i = 0; i < array.Rank; i++)
            {
                arrayWrapData.arrayLengths[i] = array.GetLength(i);
            }

            ulong gcHandle;
            IntPtr* p = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);


            arrayWrapData.objPtr = (byte*)p;
            p += 2;

            arrayWrapData.startItemOffcet = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);
            return arrayWrapData;
        }

    }

    unsafe class ArrayWrapRankOne : IArrayWrap
    {
        public ArrayWrapRankOne(int rank, Type elementType) : base(rank, elementType) { }
        public override object CreateArray(ref ArrayWrapOutData arrayWrapData)
        {
            int arrayMsize = arrayWrapData.length * this.elementTypeSize;
            object array = new byte[arrayMsize];
#if Use_Unsafe_Tool
            IntPtr* p = UnsafeTool.unsafeTool.ObjectToIntPtr(array);
#else
            ulong gcHandle;
            IntPtr* p = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);
#endif

            arrayWrapData.objPtr = (byte*)p;
            *p = head;
            ++p;
            *p = (IntPtr)arrayWrapData.length; ++p;
            arrayWrapData.startItemOffcet = (byte*)p;
            return array;
        }

        public override object CreateArray(int length)
        {
            int arrayMsize = length * this.elementTypeSize;
            object array = new byte[arrayMsize];
#if Use_Unsafe_Tool
            IntPtr* p = UnsafeTool.unsafeTool.ObjectToIntPtr(array);
#else
            ulong gcHandle;
            IntPtr* p = (IntPtr*)UnsafeUtility.PinGCObjectAndGetAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);
#endif
            *p = head;
            ++p;
            *p = (IntPtr)length; ++p;
            return array;
        }


        public override ArrayWrapOutData GetArrayData(Array array)
        {
            ArrayWrapOutData arrayWrapData = new ArrayWrapOutData();
            arrayWrapData.length = array.Length;
            ulong gcHandle;
#if Use_Unsafe_Tool
            arrayWrapData.objPtr = UnsafeTool.unsafeTool.ObjectToVoidPtr(array);
#else
            arrayWrapData.objPtr = UnsafeUtility.PinGCObjectAndGetAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);
#endif

            arrayWrapData.startItemOffcet = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(array, out gcHandle);
            UnsafeUtility.ReleaseGCObject(gcHandle);

            return arrayWrapData;
        }

    }


}
