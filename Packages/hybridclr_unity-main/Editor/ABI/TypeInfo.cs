using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HybridCLR.Editor.ABI
{
    public class TypeInfo : IEquatable<TypeInfo>
    {

        public static readonly TypeInfo s_void = new TypeInfo(ParamOrReturnType.VOID);
        public static readonly TypeInfo s_i1 = new TypeInfo(ParamOrReturnType.I1);
        public static readonly TypeInfo s_u1 = new TypeInfo(ParamOrReturnType.U1);
        public static readonly TypeInfo s_i2 = new TypeInfo(ParamOrReturnType.I2);
        public static readonly TypeInfo s_u2 = new TypeInfo(ParamOrReturnType.U2);
        public static readonly TypeInfo s_i4 = new TypeInfo(ParamOrReturnType.I4);
        public static readonly TypeInfo s_u4 = new TypeInfo(ParamOrReturnType.U4);
        public static readonly TypeInfo s_i8 = new TypeInfo(ParamOrReturnType.I8);
        public static readonly TypeInfo s_u8 = new TypeInfo(ParamOrReturnType.U8);
        public static readonly TypeInfo s_r4 = new TypeInfo(ParamOrReturnType.R4);
        public static readonly TypeInfo s_r8 = new TypeInfo(ParamOrReturnType.R8);
        public static readonly TypeInfo s_i16 = new TypeInfo(ParamOrReturnType.I16);
        public static readonly TypeInfo s_ref = new TypeInfo(ParamOrReturnType.STRUCTURE_AS_REF_PARAM);

        public static readonly TypeInfo s_vf2 = new TypeInfo(ParamOrReturnType.ARM64_HFA_FLOAT_2);
        public static readonly TypeInfo s_vf3 = new TypeInfo(ParamOrReturnType.ARM64_HFA_FLOAT_3);
        public static readonly TypeInfo s_vf4 = new TypeInfo(ParamOrReturnType.ARM64_HFA_FLOAT_4);
        public static readonly TypeInfo s_vd2 = new TypeInfo(ParamOrReturnType.ARM64_HFA_DOUBLE_2);
        public static readonly TypeInfo s_vd3 = new TypeInfo(ParamOrReturnType.ARM64_HFA_DOUBLE_3);
        public static readonly TypeInfo s_vd4 = new TypeInfo(ParamOrReturnType.ARM64_HFA_DOUBLE_4);

        public TypeInfo(ParamOrReturnType portype)
        {
            PorType = portype;
            Size = 0;
        }

        public TypeInfo(ParamOrReturnType portype, int size)
        {
            PorType = portype;
            Size = size;
        }

        public ParamOrReturnType PorType { get; }

        public bool IsGeneralValueType => PorType >= ParamOrReturnType.STRUCTURE_ALIGN1 && PorType <= ParamOrReturnType.STRUCTURE_ALIGN8;

        public int Size { get; }

        public bool Equals(TypeInfo other)
        {
            return PorType == other.PorType && Size == other.Size;
        }

        public override bool Equals(object obj)
        {
            return Equals((TypeInfo)obj);
        }

        public override int GetHashCode()
        {
            return (int)PorType * 23 + Size;
        }

        public string CreateSigName()
        {
            switch (PorType)
            {
                case ParamOrReturnType.VOID: return "v";
                case ParamOrReturnType.I1: return "i1";
                case ParamOrReturnType.U1: return "u1";
                case ParamOrReturnType.I2: return "i2";
                case ParamOrReturnType.U2: return "u2";
                case ParamOrReturnType.I4: return "i4";
                case ParamOrReturnType.U4: return "u4";
                case ParamOrReturnType.I8: return "i8";
                case ParamOrReturnType.U8: return "u8";
                case ParamOrReturnType.R4: return "r4";
                case ParamOrReturnType.R8: return "r8";
                case ParamOrReturnType.I16: return "i16";
                case ParamOrReturnType.STRUCTURE_AS_REF_PARAM: return "sr";
                case ParamOrReturnType.ARM64_HFA_FLOAT_2: return "vf2";
                case ParamOrReturnType.ARM64_HFA_FLOAT_3: return "vf3";
                case ParamOrReturnType.ARM64_HFA_FLOAT_4: return "vf4";
                case ParamOrReturnType.ARM64_HFA_DOUBLE_2: return "vd2";
                case ParamOrReturnType.ARM64_HFA_DOUBLE_3: return "vd3";
                case ParamOrReturnType.ARM64_HFA_DOUBLE_4: return "vd4";
                case ParamOrReturnType.STRUCTURE_ALIGN1: return "S" + Size;
                case ParamOrReturnType.STRUCTURE_ALIGN2: return "A" + Size;
                case ParamOrReturnType.STRUCTURE_ALIGN4: return "B" + Size;
                case ParamOrReturnType.STRUCTURE_ALIGN8: return "C" + Size;
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN1: return "X" + Size;
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN2: return "Y" + Size;
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN4: return "Z" + Size;
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN8: return "W" + Size;
                default: throw new NotSupportedException(PorType.ToString());
            };
        }

        public string GetTypeName()
        {
            switch (PorType)
            {
                case ParamOrReturnType.VOID: return "void";
                case ParamOrReturnType.I1: return "int8_t";
                case ParamOrReturnType.U1: return "uint8_t";
                case ParamOrReturnType.I2: return "int16_t";
                case ParamOrReturnType.U2: return "uint16_t";
                case ParamOrReturnType.I4: return "int32_t";
                case ParamOrReturnType.U4: return "uint32_t";
                case ParamOrReturnType.I8: return "int64_t";
                case ParamOrReturnType.U8: return "uint64_t";
                case ParamOrReturnType.R4: return "float";
                case ParamOrReturnType.R8: return "double";
                case ParamOrReturnType.I16: return "ValueTypeSize16";
                case ParamOrReturnType.STRUCTURE_AS_REF_PARAM: return "uint64_t";
                case ParamOrReturnType.ARM64_HFA_FLOAT_2: return "HtVector2f";
                case ParamOrReturnType.ARM64_HFA_FLOAT_3: return "HtVector3f";
                case ParamOrReturnType.ARM64_HFA_FLOAT_4: return "HtVector4f";
                case ParamOrReturnType.ARM64_HFA_DOUBLE_2: return "HtVector2d";
                case ParamOrReturnType.ARM64_HFA_DOUBLE_3: return "HtVector3d";
                case ParamOrReturnType.ARM64_HFA_DOUBLE_4: return "HtVector4d";
                case ParamOrReturnType.STRUCTURE_ALIGN1: return $"ValueTypeSize<{Size}>";
                case ParamOrReturnType.STRUCTURE_ALIGN2: return $"ValueTypeSizeAlign2<{Size}>";
                case ParamOrReturnType.STRUCTURE_ALIGN4: return $"ValueTypeSizeAlign4<{Size}>";
                case ParamOrReturnType.STRUCTURE_ALIGN8: return $"ValueTypeSizeAlign8<{Size}>";
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN1: return $"WebGLSpeicalValueType<{Size}>";
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN2: return $"WebGLSpeicalValueTypeAlign2<{Size}>";
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN4: return $"WebGLSpeicalValueTypeAlign4<{Size}>";
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN8: return $"WebGLSpeicalValueTypeAlign8<{Size}>";
                default: throw new NotImplementedException(PorType.ToString());
            };
        }
        public int GetParamSlotNum()
        {
            switch (PorType)
            {
                case ParamOrReturnType.VOID: return 0;
                case ParamOrReturnType.I16: return 2;
                case ParamOrReturnType.STRUCTURE_AS_REF_PARAM: return 1;
                case ParamOrReturnType.ARM64_HFA_FLOAT_3: return 2;
                case ParamOrReturnType.ARM64_HFA_FLOAT_4: return 2;
                case ParamOrReturnType.ARM64_HFA_DOUBLE_2: return 2;
                case ParamOrReturnType.ARM64_HFA_DOUBLE_3: return 3;
                case ParamOrReturnType.ARM64_HFA_DOUBLE_4: return 4;
                case ParamOrReturnType.ARM64_HVA_8:
                case ParamOrReturnType.ARM64_HVA_16: throw new NotSupportedException();
                case ParamOrReturnType.STRUCTURE_ALIGN1:
                case ParamOrReturnType.STRUCTURE_ALIGN2:
                case ParamOrReturnType.STRUCTURE_ALIGN4:
                case ParamOrReturnType.STRUCTURE_ALIGN8:
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN1:
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN2:
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN4:
                case ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN8:
                    return (Size + 7) / 8;
                default:
                    {
                        Debug.Assert(PorType < ParamOrReturnType.STRUCT_NOT_PASS_AS_VALUE);
                        Debug.Assert(Size <= 8);
                        return 1;
                    }
            }
        }
    }
}
