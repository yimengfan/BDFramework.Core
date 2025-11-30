// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    enum EvalDataType
    {
        None,
        Int32,
        Int64,
        Float,
        Double,
        I,
        Ref,
        ValueType,
        Token,
        Unknown,
    }

    struct EvalDataTypeWithSig
    {
        public readonly EvalDataType type;
        public readonly TypeSig typeSig;
        public EvalDataTypeWithSig(EvalDataType type, TypeSig typeSig)
        {
            this.type = type;
            this.typeSig = typeSig;
        }
        public override string ToString()
        {
            return $"{type} ({typeSig})";
        }
    }

    class InstructionParameterInfo
    {
        public readonly EvalDataType op1;
        public readonly EvalDataType op2;
        public readonly EvalDataType retType;
        public InstructionParameterInfo(EvalDataType op1, EvalDataType op2, EvalDataType retType)
        {
            this.op1 = op1;
            this.op2 = op2;
            this.retType = retType;
        }
    }


    class EvalStackState
    {
        public bool visited;

        public readonly List<EvalDataTypeWithSig> inputStackDatas = new List<EvalDataTypeWithSig>();
        public readonly List<EvalDataTypeWithSig> runStackDatas = new List<EvalDataTypeWithSig>();
    }

    class EvalStackCalculator
    {
        private readonly MethodDef _method;
        private readonly ICorLibTypes _corLibTypes;
        private readonly BasicBlockCollection _basicBlocks;
        private readonly Dictionary<Instruction, InstructionParameterInfo> _instructionParameterInfos = new Dictionary<Instruction, InstructionParameterInfo>();
        private readonly Dictionary<Instruction, EvalDataType> _evalStackTopDataTypeAfterInstructions = new Dictionary<Instruction, EvalDataType>();
        private readonly Dictionary<BasicBlock, EvalStackState> _blockEvalStackStates;

        public EvalStackCalculator(MethodDef method)
        {
            _method = method;
            _corLibTypes = method.Module.CorLibTypes;
            _basicBlocks = new BasicBlockCollection(method, false);
            _blockEvalStackStates = _basicBlocks.Blocks.ToDictionary(b => b, b => new EvalStackState());

            SimulateRunAllBlocks();
        }

        public BasicBlockCollection BasicBlockCollection => _basicBlocks;

        public bool TryGetParameterInfo(Instruction inst, out InstructionParameterInfo info)
        {
            return _instructionParameterInfos.TryGetValue(inst, out info);
        }

        public bool TryGetPushResult(Instruction inst, out EvalDataType result)
        {
            return _evalStackTopDataTypeAfterInstructions.TryGetValue(inst, out result);
        }

        public EvalStackState GetEvalStackState(BasicBlock basicBlock)
        {
            return _blockEvalStackStates[basicBlock];
        }

        private void PushStack(List<EvalDataTypeWithSig> datas, TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
            case ElementType.Void: break;
            case ElementType.Boolean:
            case ElementType.Char:
            case ElementType.I1:
            case ElementType.U1:
            case ElementType.I2:
            case ElementType.U2:
            case ElementType.I4:
            case ElementType.U4:
                datas.Add(new EvalDataTypeWithSig(EvalDataType.Int32, null));
                break;
            case ElementType.I8:
            case ElementType.U8:
                datas.Add(new EvalDataTypeWithSig(EvalDataType.Int64, null));
                break;
            case ElementType.R4:
                datas.Add(new EvalDataTypeWithSig(EvalDataType.Float, null));
                break;
            case ElementType.R8:
                datas.Add(new EvalDataTypeWithSig(EvalDataType.Double, null));
                break;
            case ElementType.I:
            case ElementType.U:
            case ElementType.Ptr:
            case ElementType.FnPtr:
            case ElementType.ByRef:
                datas.Add(new EvalDataTypeWithSig(EvalDataType.I, type));
                break;
            case ElementType.String:
            case ElementType.Class:
            case ElementType.Array:
            case ElementType.SZArray:
            case ElementType.Object:
                datas.Add(new EvalDataTypeWithSig(EvalDataType.Ref, type));
                break;
            case ElementType.ValueType:
            {
                TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDefThrow();
                if (typeDef.IsEnum)
                {
                    PushStack(datas, typeDef.GetEnumUnderlyingType());
                }
                else
                {
                    PushStack(datas, new EvalDataTypeWithSig(EvalDataType.ValueType, type));
                }
                break;
            }
            case ElementType.GenericInst:
            {
                GenericInstSig genericInstSig = (GenericInstSig)type;
                TypeDef typeDef = genericInstSig.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
                if (!typeDef.IsValueType)
                {
                    PushStack(datas, new EvalDataTypeWithSig(EvalDataType.Ref, type));
                }
                else if (typeDef.IsEnum)
                {
                    PushStack(datas, typeDef.GetEnumUnderlyingType());
                }
                else
                {
                    PushStack(datas, new EvalDataTypeWithSig(EvalDataType.ValueType, type));
                }
                break;
            }
            case ElementType.TypedByRef:
            {
                // TypedByRef is a special type used in dynamic method invocation and reflection.
                // It is treated as a reference type in the evaluation stack.
                PushStack(datas, new EvalDataTypeWithSig(EvalDataType.ValueType, type));
                break;
            }
            case ElementType.Var:
            case ElementType.MVar:
                PushStack(datas, new EvalDataTypeWithSig(EvalDataType.ValueType, type));
                break;
            case ElementType.ValueArray:
            case ElementType.R:
            case ElementType.CModOpt:
            case ElementType.CModReqd:
            case ElementType.Internal:
            case ElementType.Module:
            case ElementType.Sentinel:
                PushStack(datas, EvalDataType.Unknown);
                break;

            default: throw new Exception($"Unsupported type: {type} in method: {_method.FullName}.");
            }
        }

        private void PushStack(List<EvalDataTypeWithSig> datas, ITypeDefOrRef type)
        {
            PushStack(datas, type.ToTypeSig());
        }

        private void PushStack(List<EvalDataTypeWithSig> datas, EvalDataType type)
        {
            Assert.IsTrue(type != EvalDataType.ValueType, "Cannot push EvalDataType.Value without type sig onto the stack.");
            datas.Add(new EvalDataTypeWithSig(type, null));
        }

        private void PushStack(List<EvalDataTypeWithSig> datas, EvalDataTypeWithSig type)
        {
            datas.Add(type);
        }

        private void PushStackObject(List<EvalDataTypeWithSig> datas, TypeSig type)
        {
            PushStack(datas, new EvalDataTypeWithSig(EvalDataType.Ref, type));
        }

        private void PushStackPointer(List<EvalDataTypeWithSig> datas, TypeSig type)
        {
            PushStack(datas, new EvalDataTypeWithSig(EvalDataType.I, type));
        }

        //private void PushStackObject(List<EvalDataTypeWithSig> datas)
        //{
        //    datas.Add(new EvalDataTypeWithSig(EvalDataType.Ref, _method.Module.CorLibTypes.Object));
        //}

        private EvalDataType CalcBasicBinOpRetType(EvalDataType op1, EvalDataType op2)
        {
            switch (op1)
            {
            case EvalDataType.Int32:
            {
                switch (op2)
                {
                case EvalDataType.Int32: return EvalDataType.Int32;
                case EvalDataType.Int64: return EvalDataType.Int64;
                case EvalDataType.I: return EvalDataType.I;
                default: throw new Exception($"Unsupported operand type: {op2} for {op1} in binary operation.");
                }
            }
            case EvalDataType.Int64:
            {
                switch (op2)
                {
                case EvalDataType.Int32: return EvalDataType.Int64;
                case EvalDataType.Int64:
                case EvalDataType.I:
                    return EvalDataType.Int64;
                default: throw new Exception($"Unsupported operand type: {op2} for {op1} in binary operation.");
                }
            }
            case EvalDataType.I:
            {
                switch (op2)
                {
                case EvalDataType.Int32: return EvalDataType.I;
                case EvalDataType.Int64: return EvalDataType.Int64;
                case EvalDataType.I: return EvalDataType.I;
                default: throw new Exception($"Unsupported operand type: {op2} for {op1} in binary operation.");
                }
            }
            case EvalDataType.Float:
            {
                switch (op2)
                {
                case EvalDataType.Float: return EvalDataType.Float;
                case EvalDataType.Double: return EvalDataType.Double;
                default: throw new Exception($"Unsupported operand type: {op2} for {op1} in binary operation.");
                }
            }
            case EvalDataType.Double:
            {
                switch (op2)
                {
                case EvalDataType.Float:
                case EvalDataType.Double: return EvalDataType.Double;
                default: throw new Exception($"Unsupported operand type: {op2} for {op1} in binary operation.");
                }
            }
            default: throw new Exception($"Unsupported operand type: {op1} in binary operation.");
            }
        }

        private void SimulateRunAllBlocks()
        {
            bool methodHasReturnValue = !MetaUtil.IsVoidType(_method.ReturnType);

            CilBody body = _method.Body;
            if (body.HasExceptionHandlers)
            {
                foreach (ExceptionHandler handler in body.ExceptionHandlers)
                {
                    if (handler.IsFilter)
                    {
                        BasicBlock bb = _basicBlocks.GetBasicBlockByInstruction(handler.FilterStart);
                        var inputStackDatas = _blockEvalStackStates[bb].inputStackDatas;
                        if (inputStackDatas.Count == 0)
                        {
                            PushStackObject(inputStackDatas, handler.CatchType.ToTypeSig());
                        }
                    }
                    if (handler.IsCatch || handler.IsFilter)
                    {
                        BasicBlock bb = _basicBlocks.GetBasicBlockByInstruction(handler.HandlerStart);
                        var inputStackDatas = _blockEvalStackStates[bb].inputStackDatas;
                        if (inputStackDatas.Count == 0)
                        {
                            PushStackObject(inputStackDatas, handler.CatchType.ToTypeSig());
                        }
                    }
                }
            }

            var newPushedDatas = new List<EvalDataTypeWithSig>();
            IList<TypeSig> methodTypeGenericArgument = _method.DeclaringType.GenericParameters.Count > 0
                ? (IList<TypeSig>)_method.DeclaringType.GenericParameters.Select(p => (TypeSig)new GenericVar(p.Number)).ToList()
                : null;
            IList<TypeSig> methodMethodGenericArgument = _method.GenericParameters.Count > 0
                ? (IList<TypeSig>)_method.GenericParameters.Select(p => (TypeSig)new GenericMVar(p.Number)).ToList()
                : null;
            var gac = new GenericArgumentContext(methodTypeGenericArgument, methodMethodGenericArgument);
            var corLibTypes = _method.Module.CorLibTypes;

            var blockWalkStack = new Stack<BasicBlock>(_basicBlocks.Blocks.Reverse());
            while (blockWalkStack.Count > 0)
            {
                BasicBlock block = blockWalkStack.Pop();
                EvalStackState state = _blockEvalStackStates[block];
                if (state.visited)
                    continue;
                state.visited = true;
                state.runStackDatas.AddRange(state.inputStackDatas);
                List<EvalDataTypeWithSig> stackDatas = state.runStackDatas;
                foreach (var inst in block.instructions)
                {
                    int stackSize = stackDatas.Count;
                    newPushedDatas.Clear();
                    switch (inst.OpCode.Code)
                    {
                    case Code.Nop: break;
                    case Code.Break: break;
                    case Code.Ldarg_0:
                    case Code.Ldarg_1:
                    case Code.Ldarg_2:
                    case Code.Ldarg_3:
                    case Code.Ldarg:
                    case Code.Ldarg_S:
                    {
                        PushStack(newPushedDatas, inst.GetParameter(_method.Parameters).Type);
                        break;
                    }
                    case Code.Ldarga:
                    case Code.Ldarga_S:
                    {
                        PushStackPointer(newPushedDatas, new ByRefSig(inst.GetParameter(_method.Parameters).Type));
                        break;
                    }
                    case Code.Ldloc_0:
                    case Code.Ldloc_1:
                    case Code.Ldloc_2:
                    case Code.Ldloc_3:
                    case Code.Ldloc:
                    case Code.Ldloc_S:
                    {
                        PushStack(newPushedDatas, inst.GetLocal(body.Variables).Type);
                        break;
                    }
                    case Code.Ldloca:
                    case Code.Ldloca_S:
                    {
                        PushStackPointer(newPushedDatas, new ByRefSig(inst.GetLocal(body.Variables).Type));
                        break;
                    }
                    case Code.Stloc_0:
                    case Code.Stloc_1:
                    case Code.Stloc_2:
                    case Code.Stloc_3:
                    case Code.Stloc:
                    case Code.Stloc_S:
                    {
                        Assert.IsTrue(stackSize > 0);
                        break;
                    }
                    case Code.Starg:
                    case Code.Starg_S:
                    {
                        Assert.IsTrue(stackSize > 0);
                        break;
                    }
                    case Code.Ldnull:
                    {
                        PushStackObject(newPushedDatas, corLibTypes.Object);
                        break;
                    }
                    case Code.Ldc_I4_M1:
                    case Code.Ldc_I4_0:
                    case Code.Ldc_I4_1:
                    case Code.Ldc_I4_2:
                    case Code.Ldc_I4_3:
                    case Code.Ldc_I4_4:
                    case Code.Ldc_I4_5:
                    case Code.Ldc_I4_6:
                    case Code.Ldc_I4_7:
                    case Code.Ldc_I4_8:
                    case Code.Ldc_I4:
                    case Code.Ldc_I4_S:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int32);
                        break;
                    }
                    case Code.Ldc_I8:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int64);
                        break;
                    }
                    case Code.Ldc_R4:
                    {
                        PushStack(newPushedDatas, EvalDataType.Float);
                        break;
                    }
                    case Code.Ldc_R8:
                    {
                        PushStack(newPushedDatas, EvalDataType.Double);
                        break;
                    }
                    case Code.Dup:
                    {
                        Assert.IsTrue(stackSize > 0);
                        EvalDataTypeWithSig type = stackDatas[stackSize - 1];
                        PushStack(newPushedDatas, type);
                        PushStack(newPushedDatas, type);
                        break;
                    }
                    case Code.Pop:
                    {
                        break;
                    }
                    case Code.Jmp:
                    {
                        break;
                    }
                    case Code.Call:
                    case Code.Callvirt:
                    {
                        IMethod calledMethod = (IMethod)inst.Operand;
                        MethodSig methodSig = MetaUtil.GetInflatedMethodSig(calledMethod, gac);
                        PushStack(newPushedDatas, methodSig.RetType);
                        break;
                    }
                    case Code.Calli:
                    {
                        MethodSig methodSig = (MethodSig)inst.Operand;
                        PushStack(newPushedDatas, methodSig.RetType);
                        break;
                    }
                    case Code.Ret:
                    {
                        break;
                    }
                    case Code.Br:
                    case Code.Br_S:
                    case Code.Brfalse:
                    case Code.Brfalse_S:
                    case Code.Brtrue:
                    case Code.Brtrue_S:
                    case Code.Beq:
                    case Code.Beq_S:
                    case Code.Bge:
                    case Code.Bge_S:
                    case Code.Bge_Un:
                    case Code.Bge_Un_S:
                    case Code.Bgt:
                    case Code.Bgt_S:
                    case Code.Bgt_Un:
                    case Code.Bgt_Un_S:
                    case Code.Ble:
                    case Code.Ble_S:
                    case Code.Ble_Un:
                    case Code.Ble_Un_S:
                    case Code.Blt:
                    case Code.Blt_S:
                    case Code.Blt_Un:
                    case Code.Blt_Un_S:
                    case Code.Bne_Un:
                    case Code.Bne_Un_S:
                    {
                        // Branch instructions do not change the stack.
                        break;
                    }
                    case Code.Ceq:
                    case Code.Cgt:
                    case Code.Cgt_Un:
                    case Code.Clt:
                    case Code.Clt_Un:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        EvalDataType op2 = stackDatas[stackSize - 1].type;
                        EvalDataType op1 = stackDatas[stackSize - 2].type;
                        EvalDataType ret = EvalDataType.Int32;
                        _instructionParameterInfos.Add(inst, new InstructionParameterInfo(op1, op2, ret));
                        PushStack(newPushedDatas, ret);
                        break;
                    }
                    case Code.Switch:
                    {
                        // Switch instruction does not change the stack.
                        break;
                    }
                    case Code.Ldind_I1:
                    case Code.Ldind_U1:
                    case Code.Ldind_I2:
                    case Code.Ldind_U2:
                    case Code.Ldind_I4:
                    case Code.Ldind_U4:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, EvalDataType.Int32);
                        break;
                    }
                    case Code.Ldind_I8:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, EvalDataType.Int64);
                        break;
                    }
                    case Code.Ldind_I:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Ldind_Ref:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStackObject(newPushedDatas, stackDatas[stackSize - 1].typeSig?.RemovePinnedAndModifiers().Next ?? corLibTypes.Object);
                        break;
                    }
                    case Code.Ldind_R4:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, EvalDataType.Float);
                        break;
                    }
                    case Code.Ldind_R8:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, EvalDataType.Double);
                        break;
                    }
                    case Code.Stind_I1:
                    case Code.Stind_I2:
                    case Code.Stind_I4:
                    case Code.Stind_I8:
                    case Code.Stind_I:
                    case Code.Stind_R4:
                    case Code.Stind_R8:
                    case Code.Stind_Ref:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        break;
                    }

                    case Code.Add:
                    case Code.Add_Ovf:
                    case Code.Add_Ovf_Un:
                    case Code.Sub:
                    case Code.Sub_Ovf:
                    case Code.Sub_Ovf_Un:
                    case Code.Mul:
                    case Code.Mul_Ovf:
                    case Code.Mul_Ovf_Un:
                    case Code.Div:
                    case Code.Div_Un:
                    case Code.Rem:
                    case Code.Rem_Un:

                    case Code.And:
                    case Code.Or:
                    case Code.Xor:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        EvalDataType op2 = stackDatas[stackSize - 1].type;
                        EvalDataType op1 = stackDatas[stackSize - 2].type;
                        EvalDataType ret = CalcBasicBinOpRetType(op1, op2);
                        _instructionParameterInfos.Add(inst, new InstructionParameterInfo(op1, op2, ret));
                        PushStack(newPushedDatas, ret);
                        break;
                    }
                    case Code.Shl:
                    case Code.Shr:
                    case Code.Shr_Un:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        EvalDataType op2 = stackDatas[stackSize - 1].type;
                        EvalDataType op1 = stackDatas[stackSize - 2].type;
                        if (op1 != EvalDataType.Int32 && op1 != EvalDataType.Int64 && op1 != EvalDataType.I)
                            throw new Exception($"Unsupported operand type: {op1} in shift operation.");
                        if (op2 != EvalDataType.Int32 && op2 != EvalDataType.Int64)
                            throw new Exception($"Unsupported operand type: {op2} for {op1} in shift operation.");
                        EvalDataType ret = op1;
                        _instructionParameterInfos.Add(inst, new InstructionParameterInfo(op1, op2, ret));
                        PushStack(newPushedDatas, ret);
                        break;
                    }
                    case Code.Neg:
                    {
                        Assert.IsTrue(stackSize > 0);
                        EvalDataType op = stackDatas[stackSize - 1].type;
                        EvalDataType ret = op;
                        switch (op)
                        {
                        case EvalDataType.Int32:
                        case EvalDataType.Int64:
                        case EvalDataType.I:
                        case EvalDataType.Float:
                        case EvalDataType.Double:
                            break;
                        default:
                            throw new Exception($"Unsupported operand type: {op} in unary operation.");
                        }
                        _instructionParameterInfos.Add(inst, new InstructionParameterInfo(op, EvalDataType.None, ret));
                        PushStack(newPushedDatas, ret);
                        break;
                    }
                    case Code.Not:
                    {
                        Assert.IsTrue(stackSize > 0);
                        EvalDataType op = stackDatas[stackSize - 1].type;
                        EvalDataType ret = op;
                        if (op != EvalDataType.Int32 && op != EvalDataType.Int64 && op != EvalDataType.I)
                            throw new Exception($"Unsupported operand type: {op} in unary operation.");
                        _instructionParameterInfos.Add(inst, new InstructionParameterInfo(op, EvalDataType.None, ret));
                        PushStack(newPushedDatas, ret);
                        break;
                    }
                    case Code.Conv_I1:
                    case Code.Conv_U1:
                    case Code.Conv_I2:
                    case Code.Conv_U2:
                    case Code.Conv_I4:
                    case Code.Conv_U4:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int32);
                        break;
                    }
                    case Code.Conv_I8:
                    case Code.Conv_U8:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int64);
                        break;
                    }
                    case Code.Conv_I:
                    case Code.Conv_U:
                    {
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Conv_R4:
                    {
                        PushStack(newPushedDatas, EvalDataType.Float);
                        break;
                    }
                    case Code.Conv_R8:
                    {
                        PushStack(newPushedDatas, EvalDataType.Double);
                        break;
                    }
                    case Code.Conv_Ovf_I1:
                    case Code.Conv_Ovf_I1_Un:
                    case Code.Conv_Ovf_U1:
                    case Code.Conv_Ovf_U1_Un:
                    case Code.Conv_Ovf_I2:
                    case Code.Conv_Ovf_I2_Un:
                    case Code.Conv_Ovf_U2:
                    case Code.Conv_Ovf_U2_Un:
                    case Code.Conv_Ovf_I4:
                    case Code.Conv_Ovf_I4_Un:
                    case Code.Conv_Ovf_U4:
                    case Code.Conv_Ovf_U4_Un:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int32);
                        break;
                    }
                    case Code.Conv_Ovf_I8:
                    case Code.Conv_Ovf_I8_Un:
                    case Code.Conv_Ovf_U8:
                    case Code.Conv_Ovf_U8_Un:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int64);
                        break;
                    }
                    case Code.Conv_Ovf_I:
                    case Code.Conv_Ovf_I_Un:
                    case Code.Conv_Ovf_U:
                    case Code.Conv_Ovf_U_Un:
                    {
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Conv_R_Un:
                    {
                        PushStack(newPushedDatas, EvalDataType.Double);
                        break;
                    }
                    case Code.Cpobj:
                    case Code.Initobj:
                    case Code.Stobj:
                    {
                        break;
                    }
                    case Code.Ldobj:
                    {
                        PushStack(newPushedDatas, (ITypeDefOrRef)inst.Operand);
                        break;
                    }
                    case Code.Ldstr:
                    {
                        PushStackObject(newPushedDatas, corLibTypes.String);
                        break;
                    }
                    case Code.Newobj:
                    {
                        IMethod ctor = (IMethod)inst.Operand;
                        PushStack(newPushedDatas, ctor.DeclaringType);
                        break;
                    }
                    case Code.Castclass:
                    {
                        Assert.IsTrue(stackSize > 0);
                        var obj = stackDatas[stackSize - 1];
                        //Assert.IsTrue(obj.type == EvalDataType.Ref);
                        PushStackObject(newPushedDatas, ((ITypeDefOrRef)inst.Operand).ToTypeSig());
                        break;
                    }
                    case Code.Isinst:
                    {
                        Assert.IsTrue(stackSize > 0);
                        var obj = stackDatas[stackSize - 1];
                        //Assert.IsTrue(obj.type == EvalDataType.Ref);
                        PushStackObject(newPushedDatas, ((ITypeDefOrRef)inst.Operand).ToTypeSig());
                        break;
                    }
                    case Code.Unbox:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Unbox_Any:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, (ITypeDefOrRef)inst.Operand);
                        break;
                    }
                    case Code.Box:
                    {
                        Assert.IsTrue(stackSize > 0);
                        //PushStack(newPushedDatas, (ITypeDefOrRef)inst.Operand);
                        PushStackObject(newPushedDatas, ((ITypeDefOrRef)inst.Operand).ToTypeSig());
                        break;
                    }
                    case Code.Throw:
                    {
                        // Throw instruction does not change the stack.
                        break;
                    }
                    case Code.Rethrow:
                    {
                        // Rethrow instruction does not change the stack.
                        break;
                    }
                    case Code.Ldfld:
                    case Code.Ldsfld:
                    {
                        IField field = (IField)inst.Operand;
                        TypeSig fieldType = MetaUtil.InflateFieldSig(field, gac);
                        PushStack(newPushedDatas, fieldType);
                        break;
                    }
                    case Code.Ldflda:
                    case Code.Ldsflda:
                    {
                        IField field = (IField)inst.Operand;
                        TypeSig fieldType = MetaUtil.InflateFieldSig(field, gac);
                        PushStackPointer(newPushedDatas, new ByRefSig(fieldType));
                        break;
                    }
                    case Code.Stfld:
                    case Code.Stsfld:
                    {
                        break;
                    }
                    case Code.Newarr:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, new SZArraySig(((ITypeDefOrRef)inst.Operand).ToTypeSig()));
                        break;
                    }
                    case Code.Ldlen:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStack(newPushedDatas, EvalDataType.I);
                        break;
                    }
                    case Code.Ldelema:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStack(newPushedDatas, EvalDataType.I);
                        break;
                    }
                    case Code.Ldelem_I1:
                    case Code.Ldelem_U1:
                    case Code.Ldelem_I2:
                    case Code.Ldelem_U2:
                    case Code.Ldelem_I4:
                    case Code.Ldelem_U4:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStack(newPushedDatas, EvalDataType.Int32);
                        break;
                    }
                    case Code.Ldelem_I8:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStack(newPushedDatas, EvalDataType.Int64);
                        break;
                    }
                    case Code.Ldelem_I:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Ldelem_R4:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStack(newPushedDatas, EvalDataType.Float);
                        break;
                    }
                    case Code.Ldelem_R8:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStack(newPushedDatas, EvalDataType.Double);
                        break;
                    }
                    case Code.Ldelem_Ref:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStackObject(newPushedDatas, stackDatas[stackSize - 2].typeSig.RemovePinnedAndModifiers().Next ?? corLibTypes.Object);
                        break;
                    }
                    case Code.Ldelem:
                    {
                        Assert.IsTrue(stackSize >= 2);
                        PushStack(newPushedDatas, (ITypeDefOrRef)inst.Operand);
                        break;
                    }
                    case Code.Stelem_I1:
                    case Code.Stelem_I2:
                    case Code.Stelem_I4:
                    case Code.Stelem_I8:
                    case Code.Stelem_I:
                    case Code.Stelem_R4:
                    case Code.Stelem_R8:
                    case Code.Stelem_Ref:
                    case Code.Stelem:
                    {
                        Assert.IsTrue(stackSize >= 3);
                        break;
                    }
                    case Code.Mkrefany:
                    {
                        PushStack(newPushedDatas, new EvalDataTypeWithSig(EvalDataType.ValueType, _method.Module.CorLibTypes.TypedReference));
                        break;
                    }
                    case Code.Refanytype:
                    {
                        PushStack(newPushedDatas, EvalDataType.Token);
                        break;
                    }
                    case Code.Refanyval:
                    {
                        Assert.IsTrue(stackSize > 0);
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Ldtoken:
                    {
                        PushStack(newPushedDatas, EvalDataType.Token);
                        break;
                    }
                    case Code.Endfinally:
                    case Code.Leave:
                    case Code.Leave_S:
                    {
                        break;
                    }
                    case Code.Endfilter:
                    {
                        break;
                    }
                    case Code.Arglist:
                    {
                        break;
                    }
                    case Code.Ldftn:
                    case Code.Ldvirtftn:
                    {
                        PushStack(newPushedDatas, EvalDataType.Unknown);
                        break;
                    }
                    case Code.Localloc:
                    {
                        PushStackPointer(newPushedDatas, corLibTypes.IntPtr);
                        break;
                    }
                    case Code.Unaligned:
                    case Code.Volatile:
                    case Code.Tailcall:
                    case Code.No:
                    case Code.Readonly:
                    case Code.Constrained:
                    {
                        break;
                    }
                    case Code.Cpblk:
                    case Code.Initblk:
                    {
                        break;
                    }
                    case Code.Sizeof:
                    {
                        PushStack(newPushedDatas, EvalDataType.Int32);
                        break;
                    }
                    default: throw new Exception($"not supported opcode: {inst} in method: {_method.FullName}.");
                    }

                    inst.CalculateStackUsage(methodHasReturnValue, out var pushed, out var pops);
                    if (pushed != newPushedDatas.Count)
                    {
                        throw new Exception($"Instruction {inst} in method {_method.FullName} pushed {newPushedDatas.Count} items, but expected {pushed} items.");
                    }
                    if (pops == -1)
                    {
                        stackDatas.Clear();
                    }
                    else
                    {
                        if (stackSize < pops)
                        {
                            throw new Exception($"Instruction {inst} in method {_method.FullName} pops {pops} items, but only {stackSize} items are available on the stack.");
                        }
                        stackDatas.RemoveRange(stackDatas.Count - pops, pops);
                        stackDatas.AddRange(newPushedDatas);
                        Assert.AreEqual(stackSize + pushed - pops, stackDatas.Count);
                    }
                    if (pushed > 0 && stackDatas.Count > 0)
                    {
                        _evalStackTopDataTypeAfterInstructions[inst] = stackDatas.Last().type;
                    }
                }

                ReduceOutputArgsToBaseType(block, stackDatas);

                foreach (BasicBlock outBb in block.outBlocks)
                {
                    EvalStackState outState = _blockEvalStackStates[outBb];
                    if (!outState.visited)
                    {
                        blockWalkStack.Push(outBb);
                    }
                }
            }
        }

        private void ReduceOutputArgsToBaseType(BasicBlock block, List<EvalDataTypeWithSig> stackDatas)
        {
            foreach (BasicBlock outBb in block.outBlocks)
            {
                if (outBb == block)
                {
                    continue;
                }
                EvalStackState outState = _blockEvalStackStates[outBb];
                if (outState.inputStackDatas.Count == stackDatas.Count)
                {
                    ReduceOutputArgsToBaseType(stackDatas, outState.inputStackDatas);
                }
                else
                {
                    if (outState.inputStackDatas.Count > 0 || outState.visited)
                    {
                        throw new Exception($"Block {outBb} in method {_method.FullName} has inconsistent stack data. Expected {outState.inputStackDatas.Count}, but got {stackDatas.Count}.");
                    }
                }
            }
            foreach (BasicBlock outBb in block.outBlocks)
            {
                if (outBb == block)
                {
                    continue;
                }
                EvalStackState outState = _blockEvalStackStates[outBb];
                outState.inputStackDatas.Clear();
                outState.inputStackDatas.AddRange(stackDatas);
            }
        }

        private TypeSig ReduceBaseType(TypeSig type1, TypeSig type2, bool forceRefType)
        {
            if (TypeEqualityComparer.Instance.Equals(type1, type2))
            {
                return type1;
            }
            type1 = type1.RemovePinnedAndModifiers();
            type2 = type2.RemovePinnedAndModifiers();

            ElementType t1 = type1.ElementType;
            ElementType t2 = type2.ElementType;
            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                (type1, type2) = (type2, type1);
            }
            switch (t1)
            {
            case ElementType.Void:
            case ElementType.TypedByRef:
            case ElementType.ValueArray:
            {
                if (t1 != t2)
                {
                    break;
                }
                return type1;
            }
            case ElementType.R4:
            case ElementType.R8:
            case ElementType.R:
            {
                if (t1 == t2)
                {
                    return type1;
                }
                if (forceRefType)
                {
                    switch (t2)
                    {
                    case ElementType.Class:
                    case ElementType.GenericInst:
                        return type2;
                    default: return _corLibTypes.Object;
                    }
                }
                break;
            }
            case ElementType.Boolean:
            case ElementType.Char:
            case ElementType.I1:
            case ElementType.U1:
            case ElementType.I2:
            case ElementType.U2:
            case ElementType.I4:
            case ElementType.U4:
            {
                switch (t2)
                {
                case ElementType.Boolean:
                case ElementType.Char:
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                {
                    return _corLibTypes.Int32;
                }
                case ElementType.I:
                case ElementType.U:
                case ElementType.I8:
                case ElementType.U8:
                {
                    return type2;
                }
                default:
                {
                    if (forceRefType)
                    {
                        switch (t2)
                        {
                        case ElementType.Class:
                        case ElementType.GenericInst:
                            return type2;
                        default: return _corLibTypes.Object;
                        }
                    }
                    break;
                }
                }
                break;
            }
            case ElementType.I8:
            case ElementType.U8:
            {
                switch (t2)
                {
                case ElementType.I:
                case ElementType.U:
                case ElementType.I8:
                case ElementType.U8:
                {
                    return type1;
                }
                default:
                {
                    if (forceRefType)
                    {
                        switch (t2)
                        {
                        case ElementType.Class:
                        case ElementType.GenericInst:
                            return type2;
                        default: return _corLibTypes.Object;
                        }
                    }
                    break;
                }
                }
                break;
            }
            case ElementType.I:
            case ElementType.U:
            {
                switch (t2)
                {
                case ElementType.I:
                case ElementType.U:
                {
                    return type1;
                }
                case ElementType.FnPtr:
                {
                    return type2;
                }
                default:
                {
                    if (forceRefType)
                    {
                        switch (t2)
                        {
                        case ElementType.Class:
                        case ElementType.GenericInst:
                            return type2;
                        default: return _corLibTypes.Object;
                        }
                    }
                    break;
                }
                }
                break;
            }
            case ElementType.String:
            {
                if (t1 == t2)
                {
                    return type1;
                }
                return _corLibTypes.Object;
            }
            case ElementType.Ptr:
            {
                switch (t2)
                {
                case ElementType.Ptr: return new PtrSig(ReduceBaseType(type1.Next, type2.Next, false));
                case ElementType.ByRef: return new ByRefSig(ReduceBaseType(type1.Next, type2.Next, false));
                case ElementType.I:
                case ElementType.U: return type1;
                }
                break;
            }
            case ElementType.ByRef:
            {
                switch (t2)
                {
                case ElementType.ByRef: return new ByRefSig(ReduceBaseType(type1.Next, type2.Next, false));
                case ElementType.I:
                case ElementType.U: return type1;
                }
                break;
            }
            case ElementType.ValueType:
            {
                if (forceRefType)
                {
                    return type2;
                }
                break;
            }
            case ElementType.Class:
            {
                switch (t2)
                {
                case ElementType.Class:
                {
                    TypeDef typeDef1 = type1.ToTypeDefOrRef().ResolveTypeDefThrow();
                    TypeDef typeDef2 = type2.ToTypeDefOrRef().ResolveTypeDefThrow();
                    while (typeDef2 != null)
                    {
                        if (MetaUtil.IsAssignableFrom(typeDef1, typeDef2))
                        {
                            return _method.Module.Import(typeDef2).ToTypeSig();
                        }
                        typeDef2 = typeDef2.BaseType?.ResolveTypeDefThrow();
                    }
                    return _corLibTypes.Object;
                }
                case ElementType.Array:
                case ElementType.SZArray:
                {
                    TypeDef typeDef1 = type1.ToTypeDefOrRef().ResolveTypeDefThrow();
                    if (typeDef1.Namespace.StartsWith("System"))
                    {
                        switch (typeDef1.Name.ToString())
                        {
                        case "Array":
                        case "ICloneable":
                        case "IList":
                        case "IList`1":
                        case "ICollection":
                        case "ICollection`1":
                        case "IEnumerable":
                        case "IEnumerable`1":
                        case "IStructuralComparable":
                        case "IStructuralEquatable":
                        {
                            return type1;
                        }
                        }
                    }
                    return _corLibTypes.Object;
                }
                case ElementType.GenericInst:
                {
                    return type2;
                }
                case ElementType.Var:
                case ElementType.Object:
                case ElementType.MVar:
                {
                    return _corLibTypes.Object;
                }
                }
                break;
            }
            case ElementType.Array:
            {
                switch (t2)
                {
                case ElementType.Array:
                    return new ArraySig(ReduceBaseType(type1.Next, type2.Next, false));
                case ElementType.SZArray:
                    return _method.Module.ImportAsTypeSig(typeof(Array));
                case ElementType.GenericInst:
                {
                    var gis = (GenericInstSig)type2;
                    switch (gis.GenericType.GetName())
                    {
                    case "IList`1":
                    case "ICollection`1":
                        return type2;
                    }
                    return _corLibTypes.Object;
                }
                case ElementType.Object:
                {
                    return _corLibTypes.Object;
                }
                }
                break;
            }
            case ElementType.GenericInst:
            {
                var gis = (GenericInstSig)type1;
                switch (t2)
                {
                case ElementType.Object:
                {
                    return _corLibTypes.Object;
                }
                case ElementType.MVar:
                {
                    return type2;
                }
                case ElementType.SZArray:
                {
                    switch (gis.GenericType.GetName())
                    {
                    case "IList`1":
                    case "ICollection`1":
                    {
                        return type1;
                    }
                    // FIXME
                    default: return _corLibTypes.Object;
                    }
                }
                case ElementType.GenericInst:
                {
                    // FIXME
                    return type1;
                }
                }
                break;
            }
            case ElementType.FnPtr:
            {
                switch (t2)
                {
                case ElementType.FnPtr:
                {
                    // FIXME
                    return type1;
                }
                }
                break;
            }
            case ElementType.Object:
            {
                return type1;
            }
            case ElementType.SZArray:
            {
                switch (t2)
                {
                case ElementType.SZArray:
                    return new SZArraySig(ReduceBaseType(type1.Next, type2.Next, false));
                case ElementType.MVar:
                    return _corLibTypes.Object;
                }
                break;
            }
            case ElementType.Var:
            case ElementType.MVar:
            {
                if (t1 == t2)
                {
                    // FIXME!!!
                    return type1;
                }
                if (t2 == ElementType.Class || t2 == ElementType.GenericInst)
                {
                    return type2;
                }
                return _corLibTypes.Object;
            }
            }
            UnityEngine.Debug.LogError($"reduce base type of {type1} and {type2} is unsupported");
            return type1;
        }

        private EvalDataTypeWithSig ReduceBaseType(EvalDataTypeWithSig type1, EvalDataTypeWithSig type2)
        {
            if (type1.type != type2.type)
            {
                throw new Exception($"Method {_method.FullName} has inconsistent stack data. Expected {type1.type}, but got {type2.type}.");
            }
            switch (type1.type)
            {
            case EvalDataType.Int32:
            case EvalDataType.Int64:
            case EvalDataType.Float:
            case EvalDataType.Double:
            case EvalDataType.Token:
            case EvalDataType.Unknown:
            {
                return type1;
            }
            case EvalDataType.I:
            case EvalDataType.Ref:
            {
                if (type1.typeSig == null)
                {
                    return type2;
                }
                if (type2.typeSig == null)
                {
                    return type1;
                }
                if (TypeEqualityComparer.Instance.Equals(type1.typeSig, type2.typeSig))
                {
                    return type1;
                }
                return new EvalDataTypeWithSig(type1.type, ReduceBaseType(type1.typeSig, type2.typeSig, type1.type == EvalDataType.Ref));
            }
            case EvalDataType.ValueType:
            {
                if (!TypeEqualityComparer.Instance.Equals(type1.typeSig, type2.typeSig))
                {
                    throw new Exception($"Method {_method.FullName} has inconsistent stack data. Expected {type1.typeSig}, but got {type2.typeSig}.");
                }
                return type1;
            }
            default: throw new Exception($"not supported EvalDataType: {type1.type} in method: {_method.FullName}.");
            }
        }

        private void ReduceOutputArgsToBaseType(List<EvalDataTypeWithSig> stackDatasRet, List<EvalDataTypeWithSig> stackDatasNew)
        {
            if (stackDatasNew.Count == 0)
            {
                return;
            }
            if (stackDatasRet.Count != stackDatasNew.Count)
            {
                throw new Exception($"Method {_method.FullName} has inconsistent stack data. Expected {stackDatasRet.Count}, but got {stackDatasNew.Count}.");
            }
            for (int i = 0; i < stackDatasRet.Count; i++)
            {
                EvalDataTypeWithSig oldType = stackDatasRet[i];
                EvalDataTypeWithSig newType = stackDatasNew[i];
                stackDatasRet[i] = ReduceBaseType(oldType, newType);
            }
        }
    }
}
