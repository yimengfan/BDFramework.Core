using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.ABI
{
    public abstract class TypeCreatorBase
    {
        public abstract bool IsArch32 { get; }

        public virtual bool IsSupportHFA => false;

        public virtual bool IsSupportWebGLSpecialValueType => false;

        public TypeInfo GetNativeIntTypeInfo() => IsArch32 ? TypeInfo.s_i4 : TypeInfo.s_i8;

        public ValueTypeSizeAligmentCalculator Calculator => IsArch32 ? ValueTypeSizeAligmentCalculator.Caculator32 : ValueTypeSizeAligmentCalculator.Caculator64;


        private readonly Dictionary<TypeSig, (int, int)> _typeSizeCache = new Dictionary<TypeSig, (int, int)>(TypeEqualityComparer.Instance);

        public (int Size, int Aligment) ComputeSizeAndAligment(TypeSig t)
        {
            if (_typeSizeCache.TryGetValue(t, out var sizeAndAligment))
            {
                return sizeAndAligment;
            }
            sizeAndAligment = Calculator.SizeAndAligmentOf(t);
            _typeSizeCache.Add(t, sizeAndAligment);
            return sizeAndAligment;
        }

        public TypeInfo CreateTypeInfo(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            if (type.IsByRef)
            {
                return GetNativeIntTypeInfo();
            }
            switch (type.ElementType)
            {
                case ElementType.Void: return TypeInfo.s_void;
                case ElementType.Boolean: return TypeInfo.s_u1;
                case ElementType.I1: return TypeInfo.s_i1;
                case ElementType.U1: return TypeInfo.s_u1;
                case ElementType.I2: return TypeInfo.s_i2;
                case ElementType.Char:
                case ElementType.U2: return TypeInfo.s_u2;
                case ElementType.I4: return TypeInfo.s_i4;
                case ElementType.U4: return TypeInfo.s_u4;
                case ElementType.I8: return TypeInfo.s_i8;
                case ElementType.U8: return TypeInfo.s_u8;
                case ElementType.R4: return TypeInfo.s_r4;
                case ElementType.R8: return TypeInfo.s_r8;
                case ElementType.U: return IsArch32 ? TypeInfo.s_u4 : TypeInfo.s_u8;
                case ElementType.I:
                case ElementType.String:
                case ElementType.Ptr:
                case ElementType.ByRef:
                case ElementType.Class:
                case ElementType.Array:
                case ElementType.SZArray:
                case ElementType.FnPtr:
                case ElementType.Object:
                case ElementType.Module:
                case ElementType.Var:
                case ElementType.MVar:
                return GetNativeIntTypeInfo();
                case ElementType.TypedByRef: return CreateValueType(type);
                case ElementType.ValueType:
                {
                    TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef == null)
                    {
                        throw new Exception($"type:{type} 未能找到定义。请尝试 `HybridCLR/Genergate/LinkXml`，然后Build一次生成AOT dll，再重新生成桥接函数");
                    }
                    if (typeDef.IsEnum)
                    {
                        return CreateTypeInfo(typeDef.GetEnumUnderlyingType());
                    }
                    return CreateValueType(type);
                }
                case ElementType.GenericInst:
                {
                    GenericInstSig gis = (GenericInstSig)type;
                    if (!gis.GenericType.IsValueType)
                    {
                        return GetNativeIntTypeInfo();
                    }
                    TypeDef typeDef = gis.GenericType.ToTypeDefOrRef().ResolveTypeDef();
                    if (typeDef.IsEnum)
                    {
                        return CreateTypeInfo(typeDef.GetEnumUnderlyingType());
                    }
                    return CreateValueType(type);
                }
                default: throw new NotSupportedException($"{type.ElementType}");
            }
        }

        private static bool IsNotHFAFastCheck(int typeSize)
        {
            return typeSize != 8 && typeSize != 12 && typeSize != 16 && typeSize != 24 && typeSize != 32;
        }

        private static bool ComputHFATypeInfo0(TypeSig type, HFATypeInfo typeInfo)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();

            List<TypeSig> klassInst = type.ToGenericInstSig()?.GenericArguments?.ToList();
            GenericArgumentContext ctx = klassInst != null ? new GenericArgumentContext(klassInst, null) : null;

            var fields = typeDef.Fields;// typeDef.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldDef field in fields)
            {
                if (field.IsStatic)
                {
                    continue;
                }
                TypeSig ftype = ctx != null ? MetaUtil.Inflate(field.FieldType, ctx) : field.FieldType;
                switch (ftype.ElementType)
                {
                    case ElementType.R4:
                    case ElementType.R8:
                    {
                        if (ftype == typeInfo.Type || typeInfo.Type == null)
                        {
                            typeInfo.Type = ftype;
                            ++typeInfo.Count;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                    case ElementType.ValueType:
                    {
                        if (!ComputHFATypeInfo0(ftype, typeInfo))
                        {
                            return false;
                        }
                        break;
                    }
                    case ElementType.GenericInst:
                    {
                        if (!ftype.IsValueType || !ComputHFATypeInfo0(ftype, typeInfo))
                        {
                            return false;
                        }
                        break;
                    }
                    default: return false;
                }
            }
            return typeInfo.Count <= 4;
        }

        private static bool ComputHFATypeInfo(TypeSig type, int typeSize, out HFATypeInfo typeInfo)
        {
            typeInfo = new HFATypeInfo();
            if (IsNotHFAFastCheck(typeSize))
            {
                return false;
            }
            bool ok = ComputHFATypeInfo0(type, typeInfo);
            if (ok && typeInfo.Count >= 2 && typeInfo.Count <= 4)
            {
                int fieldSize = typeInfo.Type.ElementType == ElementType.R4 ? 4 : 8;
                return typeSize == fieldSize * typeInfo.Count;
            }
            return false;
        }

        public static bool TryComputSingletonStruct(TypeSig type, out SingletonStruct result)
        {
            result = new SingletonStruct();
            return TryComputSingletonStruct0(type, result) && result.Type != null;
        }

        public static bool TryComputSingletonStruct0(TypeSig type, SingletonStruct result)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();
            if (typeDef.IsEnum)
            {
                if (result.Type == null)
                {
                    result.Type = typeDef.GetEnumUnderlyingType();
                    return true;
                }
                else
                {
                    return false;
                }
            }

            List<TypeSig> klassInst = type.ToGenericInstSig()?.GenericArguments?.ToList();
            GenericArgumentContext ctx = klassInst != null ? new GenericArgumentContext(klassInst, null) : null;

            var fields = typeDef.Fields;// typeDef.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldDef field in fields)
            {
                if (field.IsStatic)
                {
                    continue;
                }
                TypeSig ftype = ctx != null ? MetaUtil.Inflate(field.FieldType, ctx) : field.FieldType;

                switch (ftype.ElementType)
                {
                    case ElementType.TypedByRef: return false;
                    case ElementType.ValueType:
                    {
                        if (!TryComputSingletonStruct0(ftype, result))
                        {
                            return false;
                        }
                        break;
                    }
                    case ElementType.GenericInst:
                    {
                        if (!ftype.IsValueType)
                        {
                            goto default;
                        }
                        if (!TryComputSingletonStruct0(ftype, result))
                        {
                            return false;
                        }
                        break;
                    }
                    default:
                    {
                        if (result.Type != null)
                        {
                            return false;
                        }
                        result.Type = ftype;
                        break;
                    }
                }
            }

            return true;
        }

        public static bool IsWebGLSpeicalValueType(TypeSig type)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();
            if (typeDef.IsEnum)
            {
                return false;
            }
            var fields = typeDef.Fields;// typeDef.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Count == 0)
            {
                return true;
            }
            return fields.All(f => f.IsStatic);
        }

        protected static TypeInfo CreateGeneralValueType(TypeSig type, int size, int aligment)
        {
            System.Diagnostics.Debug.Assert(size % aligment == 0);
            switch (aligment)
            {
                case 1: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN1, size);
                case 2: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN2, size);
                case 4: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN4, size);
                case 8: return new TypeInfo(ParamOrReturnType.STRUCTURE_ALIGN8, size);
                default: throw new NotSupportedException($"type:{type} not support aligment:{aligment}");
            }
        }

        protected TypeInfo CreateValueType(TypeSig type)
        {
            (int typeSize, int typeAligment) = ComputeSizeAndAligment(type);
            if (IsSupportHFA && ComputHFATypeInfo(type, typeSize, out HFATypeInfo hfaTypeInfo))
            {
                bool isFloat = hfaTypeInfo.Type.ElementType == ElementType.R4;
                switch (hfaTypeInfo.Count)
                {
                    case 2: return isFloat ? TypeInfo.s_vf2 : TypeInfo.s_vd2;
                    case 3: return isFloat ? TypeInfo.s_vf3 : TypeInfo.s_vd3;
                    case 4: return isFloat ? TypeInfo.s_vf4 : TypeInfo.s_vd4;
                    default: throw new NotSupportedException();
                }
            }
            if (IsSupportWebGLSpecialValueType && IsWebGLSpeicalValueType(type))
            {
                switch (typeAligment)
                {
                    case 1: return new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN1, typeSize);
                    case 2: return new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN2, typeSize);
                    case 4: return new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN4, typeSize);
                    case 8: return new TypeInfo(ParamOrReturnType.SPECIAL_STRUCTURE_ALIGN8, typeSize);
                    default: throw new NotSupportedException();
                }
            }
            else
            {
                // 64位下结构体内存对齐规则是一样的
                return CreateGeneralValueType(type, typeSize, typeAligment);
            }
        }


        protected abstract TypeInfo OptimizeSigType(TypeInfo type, bool returnType);

        public virtual void OptimizeMethod(MethodDesc method)
        {
            method.TransfromSigTypes(OptimizeSigType);
        }
    }
}
