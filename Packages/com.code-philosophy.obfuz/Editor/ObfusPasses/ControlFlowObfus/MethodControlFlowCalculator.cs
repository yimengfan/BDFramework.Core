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
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    class MethodControlFlowCalculator
    {
        class BasicBlockInputOutputArguments
        {
            public readonly List<Local> locals = new List<Local>();

            public BasicBlockInputOutputArguments()
            {
            }

            public BasicBlockInputOutputArguments(MethodDef method, List<EvalDataTypeWithSig> inputStackDatas)
            {
                ICorLibTypes corLibTypes = method.Module.CorLibTypes;
                foreach (var data in inputStackDatas)
                {
                    Local local = new Local(GetLocalTypeSig(corLibTypes, data));
                    locals.Add(local);
                    method.Body.Variables.Add(local);
                }
            }

            private TypeSig GetLocalTypeSig(ICorLibTypes corLibTypes, EvalDataTypeWithSig type)
            {
                TypeSig typeSig = type.typeSig;
                switch (type.type)
                {
                    case EvalDataType.Int32: return corLibTypes.Int32;
                    case EvalDataType.Int64: return corLibTypes.Int64;
                    case EvalDataType.Float: return corLibTypes.Single;
                    case EvalDataType.Double: return corLibTypes.Double;
                    case EvalDataType.I: return typeSig ?? corLibTypes.IntPtr;
                    case EvalDataType.Ref: return typeSig == null || MetaUtil.IsValueType(typeSig) ? corLibTypes.Object : typeSig;
                    case EvalDataType.ValueType: Assert.IsNotNull(typeSig); return typeSig;
                    case EvalDataType.Token: throw new System.NotSupportedException("Token type is not supported in BasicBlockInputOutputArguments");
                    default: throw new System.NotSupportedException("not supported EvalDataType");
                }
            }
        }

        class BasicBlockInfo
        {
            public BlockGroup group;

            //public int order;
            public bool isSaveStackBlock;
            public BasicBlockInfo prev;
            public BasicBlockInfo next;

            public List<Instruction> instructions;
            public List<EvalDataTypeWithSig> inputStackDatas;
            public List<EvalDataTypeWithSig> outputStackDatas;

            public List<BasicBlockInfo> inBasicBlocks = new List<BasicBlockInfo>();
            public List<BasicBlockInfo> outBasicBlocks = new List<BasicBlockInfo>();

            public BasicBlockInputOutputArguments inputArgs;
            public BasicBlockInputOutputArguments outputArgs;

            public Instruction FirstInstruction => instructions[0];

            public Instruction LastInstruction => instructions[instructions.Count - 1];

            public Instruction GroupFirstInstruction => group.basicBlocks[0].FirstInstruction;


            //public void InsertNext(BasicBlockInfo nextBb)
            //{
            //    if (next != null)
            //    {
            //        next.prev = nextBb;
            //        nextBb.next = next;
            //    }
            //    nextBb.prev = this;
            //    next = nextBb;
            //}

            public void InsertBefore(BasicBlockInfo prevBb)
            {
                prev.next = prevBb;
                prevBb.prev = prev;
                prevBb.next = this;
                this.prev = prevBb;
            }

            public void AddOutBasicBlock(BasicBlockInfo outBb)
            {
                if (!outBasicBlocks.Contains(outBb))
                {
                    outBasicBlocks.Add(outBb);
                    outBb.inBasicBlocks.Add(this);
                }
            }

            public void ClearInBasicBlocks()
            {
                foreach (var inBb in inBasicBlocks)
                {
                    inBb.outBasicBlocks.Remove(this);
                }
                inBasicBlocks.Clear();
            }

            public void RetargetInBasicBlocksTo(BasicBlockInfo prevBb, Dictionary<Instruction, BasicBlockInfo> inst2bb)
            {
                var oldInBlocks = new List<BasicBlockInfo>(inBasicBlocks);
                ClearInBasicBlocks();
                foreach (var oldInBb in oldInBlocks)
                {
                    oldInBb.AddOutBasicBlock(prevBb);
                }
                // inBB => saveBb => cur
                foreach (BasicBlockInfo inBb in prevBb.inBasicBlocks)
                {
                    if (inBb.instructions.Count == 0)
                    {
                        // empty block, no need to retarget
                        continue;
                    }
                    Instruction lastInst = inBb.instructions.Last();
                    if (lastInst.Operand is Instruction targetInst)
                    {
                        if (inst2bb.TryGetValue(targetInst, out BasicBlockInfo targetBb) && targetBb == this)
                        {
                            // retarget to prevBb
                            lastInst.Operand = prevBb.FirstInstruction;
                        }
                    }
                    else if (lastInst.Operand is Instruction[] targetInsts)
                    {
                        for (int i = 0; i < targetInsts.Length; i++)
                        {
                            targetInst = targetInsts[i];
                            if (inst2bb.TryGetValue(targetInst, out BasicBlockInfo targetBb) && targetBb == this)
                            {
                                targetInsts[i] = prevBb.FirstInstruction;
                            }
                        }
                    }
                }
            }
        }

        private readonly MethodDef _method;
        private readonly IRandom _random;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly int _minInstructionCountOfBasicBlockToObfuscate;
        private readonly BasicBlockInfo _bbHead;

        public MethodControlFlowCalculator(MethodDef method, IRandom random, ConstFieldAllocator constFieldAllocator, int minInstructionCountOfBasicBlockToObfuscate)
        {
            _method = method;
            _random = random;
            _constFieldAllocator = constFieldAllocator;
            _minInstructionCountOfBasicBlockToObfuscate = minInstructionCountOfBasicBlockToObfuscate;

            _bbHead = new BasicBlockInfo()
            {
                instructions = new List<Instruction>(),
                inputStackDatas = new List<EvalDataTypeWithSig>(),
                outputStackDatas = new List<EvalDataTypeWithSig>(),
            };
        }

        private void BuildBasicBlockLink(EvalStackCalculator evc)
        {
            BasicBlockInfo prev = _bbHead;
            var bb2bb = new Dictionary<BasicBlock, BasicBlockInfo>();
            foreach (BasicBlock bb in evc.BasicBlockCollection.Blocks)
            {
                EvalStackState ess = evc.GetEvalStackState(bb);
                var newBB = new BasicBlockInfo
                {
                    prev = prev,
                    next = null,
                    instructions = bb.instructions,
                    inputStackDatas = ess.inputStackDatas,
                    outputStackDatas = ess.runStackDatas,
                };
                prev.next = newBB;
                prev = newBB;
                bb2bb.Add(bb, newBB);
            }
            foreach (BasicBlock bb in evc.BasicBlockCollection.Blocks)
            {
                BasicBlockInfo bbi = bb2bb[bb];
                foreach (var inBb in bb.inBlocks)
                {
                    bbi.inBasicBlocks.Add(bb2bb[inBb]);
                }
                foreach (var outBb in bb.outBlocks)
                {
                    bbi.outBasicBlocks.Add(bb2bb[outBb]);
                }
            }

            // let _bbHead point to the first basic block
            //_bbHead.instructions.Add(Instruction.Create(OpCodes.Br, _bbHead.next.FirstInstruction));
            _bbHead.next.inBasicBlocks.Add(_bbHead);
            _bbHead.outBasicBlocks.Add(_bbHead.next);
        }

        private bool CheckNotContainsNotSupportedEvalStackData()
        {
            for (BasicBlockInfo cur = _bbHead; cur != null; cur = cur.next)
            {
                foreach (var data in cur.inputStackDatas)
                {
                    if (data.type == EvalDataType.Unknown || data.type == EvalDataType.Token)
                    {
                        Debug.LogError($"NotSupported EvalStackData found in method: {_method.FullName}, type: {data.type}");
                        return false;
                    }
                }
            }
            return true;
        }


        private void WalkInputArgumentGroup(BasicBlockInfo cur, BasicBlockInputOutputArguments inputArgs)
        {
            if (cur.inputArgs != null)
            {
                Assert.AreEqual(cur.inputArgs, inputArgs, "input arguments not match");
                return;
            }
            cur.inputArgs = inputArgs;
            foreach (BasicBlockInfo inputBB in cur.inBasicBlocks)
            {
                if (inputBB.outputArgs != null)
                {
                    Assert.AreEqual(inputBB.outputArgs, inputArgs, $"Input BB {inputBB} outputArgs does not match in method: {_method.FullName}");
                    continue;
                }
                inputBB.outputArgs = cur.inputArgs;
                foreach (var outBB in inputBB.outBasicBlocks)
                {
                    WalkInputArgumentGroup(outBB, inputArgs);
                }
            }
        }

        private readonly BasicBlockInputOutputArguments emptyEvalStackArgs = new BasicBlockInputOutputArguments();

        private void ComputeInputOutputArguments()
        {
            for (BasicBlockInfo cur = _bbHead; cur != null; cur = cur.next)
            {
                if (cur.inputArgs == null)
                {
                    if (cur.inputStackDatas.Count == 0)
                    {
                        cur.inputArgs = emptyEvalStackArgs;
                    }
                    else
                    {
                        var inputArgs = new BasicBlockInputOutputArguments(_method, cur.inputStackDatas);
                        WalkInputArgumentGroup(cur, inputArgs);
                    }
                }
                if (cur.outputArgs == null && cur.outputStackDatas.Count == 0)
                {
                    cur.outputArgs = emptyEvalStackArgs;
                }
            }
            for (BasicBlockInfo cur = _bbHead; cur != null; cur = cur.next)
            {
                if (cur.inputArgs == null)
                {
                    throw new System.Exception($"Input arguments for BasicBlock {cur} in method {_method.FullName} is null");
                }
                if (cur.outputArgs == null)
                {
                    if (cur.instructions.Count > 0)
                    {
                        Code lastInstCode = cur.LastInstruction.OpCode.Code;
                        Assert.IsTrue(lastInstCode == Code.Throw || lastInstCode == Code.Rethrow);
                        cur.outputStackDatas = new List<EvalDataTypeWithSig>();
                    }
                    cur.outputArgs = emptyEvalStackArgs;
                }
            }
        }


        private BasicBlockInfo CreateSaveStackBasicBlock(BasicBlockInfo to)
        {
            if (to.group == null)
            {
                throw new Exception($"BasicBlock {to} in method {_method.FullName} does not belong to any group. This should not happen.");
            }

            var saveLocalBasicBlock = new BasicBlockInfo
            {
                group = to.group,
                isSaveStackBlock = true,
                inputStackDatas = to.inputStackDatas,
                inputArgs = to.inputArgs,
                outputStackDatas = new List<EvalDataTypeWithSig>(),
                outputArgs = emptyEvalStackArgs,
                instructions = new List<Instruction>(),
            };

            var locals = to.inputArgs.locals;
            if (locals.Count > 0)
            {
                to.instructions.InsertRange(0, locals.Select(l => Instruction.Create(OpCodes.Ldloc, l)));

            }
            for (int i = locals.Count - 1; i >= 0; i--)
            {
                saveLocalBasicBlock.instructions.Add(Instruction.Create(OpCodes.Stloc, locals[i]));
            }

            to.inputArgs = emptyEvalStackArgs;
            to.inputStackDatas = new List<EvalDataTypeWithSig>();

            BlockGroup group = to.group;
            group.basicBlocks.Insert(group.basicBlocks.IndexOf(to), saveLocalBasicBlock);
            group.switchMachineCases.Add(new SwitchMachineCase { index = -1, prepareBlock = saveLocalBasicBlock, targetBlock = to });
            saveLocalBasicBlock.instructions.Add(Instruction.Create(OpCodes.Ldsfld, (FieldDef)null));
            saveLocalBasicBlock.instructions.Add(Instruction.Create(OpCodes.Stloc, GlobalSwitchIndexLocal));
            saveLocalBasicBlock.instructions.Add(Instruction.Create(OpCodes.Br, group.switchMachineEntryInst));


            return saveLocalBasicBlock;
        }

        private void AdjustInputOutputEvalStack()
        {
            Dictionary<Instruction, BasicBlockInfo> inst2bb = BuildInstructionToBasicBlockInfoDic();
            for (BasicBlockInfo cur = _bbHead.next; cur != null; cur = cur.next)
            {
                if (cur.inputArgs.locals.Count == 0 && cur.instructions.Count < _minInstructionCountOfBasicBlockToObfuscate)
                {
                    // small block, no need to save stack
                    continue;
                }

                BasicBlockInfo saveBb = CreateSaveStackBasicBlock(cur);
                cur.InsertBefore(saveBb);
                cur.RetargetInBasicBlocksTo(saveBb, inst2bb);
                //saveBb.AddOutBasicBlock(cur);
            }
        }

        private void InsertSwitchMachineBasicBlockForGroups(BlockGroup rootGroup)
        {
            Dictionary<Instruction, BasicBlockInfo> inst2bb = BuildInstructionToBasicBlockInfoDic();

            InsertSwitchMachineBasicBlockForGroup(rootGroup, inst2bb);
        }

        private void ShuffleBasicBlocks0(List<BasicBlockInfo> bbs)
        {
            if (bbs.Count <= 2)
            {
                return;
            }

            var subBlocksExcludeFirstLast = bbs.GetRange(1, bbs.Count - 2);

            var blocksInputArgsCountZero = new List<BasicBlockInfo>();
            var blocksInputArgsCountNonZero = new List<BasicBlockInfo>();
            foreach (var bb in subBlocksExcludeFirstLast)
            {
                if (bb.inputArgs.locals.Count == 0)
                {
                    blocksInputArgsCountZero.Add(bb);
                }
                else
                {
                    blocksInputArgsCountNonZero.Add(bb);
                }
            }
            RandomUtil.ShuffleList(blocksInputArgsCountZero, _random);

            int index = 1;
            foreach (var bb in blocksInputArgsCountZero)
            {
                bbs[index++] = bb;
            }
            foreach (var bb in blocksInputArgsCountNonZero)
            {
                bbs[index++] = bb;
            }
            Assert.AreEqual(bbs.Count - 1, index, "Shuffled basic blocks count should be the same as original count minus first and last blocks");

            //var firstSection = new List<BasicBlockInfo>() { bbs[0] };
            //var sectionsExcludeFirstLast = new List<List<BasicBlockInfo>>();
            //List<BasicBlockInfo> currentSection = firstSection;
            //for (int i = 1; i < n; i++)
            //{
            //    BasicBlockInfo cur = bbs[i];
            //    if (cur.inputArgs.locals.Count == 0)
            //    {
            //        currentSection = new List<BasicBlockInfo>() { cur };
            //        sectionsExcludeFirstLast.Add(currentSection);
            //    }
            //    else
            //    {
            //        currentSection.Add(cur);
            //    }
            //}
            //if (sectionsExcludeFirstLast.Count <= 1)
            //{
            //    return;
            //}
            //var lastSection = sectionsExcludeFirstLast.Last();
            //sectionsExcludeFirstLast.RemoveAt(sectionsExcludeFirstLast.Count - 1);


            //RandomUtil.ShuffleList(sectionsExcludeFirstLast, _random);

            //bbs.Clear();
            //bbs.AddRange(firstSection);
            //bbs.AddRange(sectionsExcludeFirstLast.SelectMany(section => section));
            //bbs.AddRange(lastSection);
            //Assert.AreEqual(n, bbs.Count, "Shuffled basic blocks count should be the same as original count");
        }

        private void ShuffleBasicBlocks(List<BasicBlockInfo> bbs)
        {
            // TODO

            int n = bbs.Count;
            BasicBlockInfo groupPrev = bbs[0].prev;
            BasicBlockInfo groupNext = bbs[n - 1].next;
            //RandomUtil.ShuffleList(bbs, _random);
            ShuffleBasicBlocks0(bbs);
            BasicBlockInfo prev = groupPrev;
            for (int i = 0; i < n; i++)
            {
                BasicBlockInfo cur = bbs[i];
                cur.prev = prev;
                prev.next = cur;
                prev = cur;
            }
            prev.next = groupNext;
            if (groupNext != null)
            {
                groupNext.prev = prev;
            }
        }

        private Local _globalSwitchIndexLocal;

        Local GlobalSwitchIndexLocal
        {
            get
            {
                if (_globalSwitchIndexLocal == null)
                {
                    _globalSwitchIndexLocal = new Local(_method.Module.CorLibTypes.Int32);
                    _method.Body.Variables.Add(_globalSwitchIndexLocal);
                }
                return _globalSwitchIndexLocal;
            }
        }

        private void InsertSwitchMachineBasicBlockForGroup(BlockGroup group, Dictionary<Instruction, BasicBlockInfo> inst2bb)
        {
            if (group.subGroups != null && group.subGroups.Count > 0)
            {
                foreach (var subGroup in group.subGroups)
                {
                    InsertSwitchMachineBasicBlockForGroup(subGroup, inst2bb);
                }
            }
            else if (group.switchMachineCases.Count > 0)
            {
                Assert.IsTrue(group.basicBlocks.Count > 0, "Group should contain at least one basic block");

                BasicBlockInfo firstBlock = group.basicBlocks[0];
                var firstCase = group.switchMachineCases[0];
                //Assert.AreEqual(firstCase.prepareBlock, firstBlock, "First case prepare block should be the first basic block in group");

                Assert.IsTrue(firstCase.targetBlock.inputArgs.locals.Count == 0);
                Assert.IsTrue(firstCase.targetBlock.inputStackDatas.Count == 0);

                var instructions = new List<Instruction>()
                    {
                        Instruction.Create(OpCodes.Ldsfld, (FieldDef)null),
                        Instruction.Create(OpCodes.Stloc, GlobalSwitchIndexLocal),
                        group.switchMachineEntryInst,
                        group.switchMachineInst,
                        Instruction.Create(OpCodes.Br, firstCase.targetBlock.FirstInstruction),
                    };
                if (firstCase.prepareBlock != firstBlock || firstBlock.inputStackDatas.Count != 0)
                {
                    instructions.Insert(0, Instruction.Create(OpCodes.Br, firstBlock.FirstInstruction));
                }

                var switchMachineBb = new BasicBlockInfo()
                {
                    group = group,
                    inputArgs = firstBlock.inputArgs,
                    outputArgs = emptyEvalStackArgs,
                    inputStackDatas = firstBlock.inputStackDatas,
                    outputStackDatas = new List<EvalDataTypeWithSig>(),
                    instructions = instructions,
                };
                firstBlock.InsertBefore(switchMachineBb);
                group.basicBlocks.Insert(0, switchMachineBb);
                ShuffleBasicBlocks(group.basicBlocks);

                List<Instruction> switchTargets = (List<Instruction>)group.switchMachineInst.Operand;

                RandomUtil.ShuffleList(group.switchMachineCases, _random);

                for (int i = 0, n = group.switchMachineCases.Count; i < n; i++)
                {
                    SwitchMachineCase switchMachineCase = group.switchMachineCases[i];
                    switchMachineCase.index = i;
                    List<Instruction> prepareBlockInstructions = switchMachineCase.prepareBlock.instructions;

                    Instruction setBranchIndexInst = prepareBlockInstructions[prepareBlockInstructions.Count - 3];
                    Assert.AreEqual(setBranchIndexInst.OpCode, OpCodes.Ldsfld, "first instruction of prepareBlock should be Ldsfld");
                    //setBranchIndexInst.Operand = i;
                    var indexField = _constFieldAllocator.Allocate(i);
                    setBranchIndexInst.Operand = indexField;
                    switchTargets.Add(switchMachineCase.targetBlock.FirstInstruction);
                }

                // after shuffle
                //Assert.IsTrue(instructions.Count == 4 || instructions.Count == 5, "Switch machine basic block should contain 4 or 5 instructions");
                Instruction loadFirstIndex = instructions[instructions.Count - 5];
                Assert.AreEqual(Code.Ldsfld, loadFirstIndex.OpCode.Code, "First instruction should be Ldsfld");
                loadFirstIndex.Operand = _constFieldAllocator.Allocate(firstCase.index);
            }
        }

        private bool IsPrevBasicBlockControlFlowNextToThis(BasicBlockInfo cur)
        {
            Instruction lastInst = cur.prev.LastInstruction;
            switch (lastInst.OpCode.FlowControl)
            {
                case FlowControl.Cond_Branch:
                case FlowControl.Call:
                case FlowControl.Next:
                case FlowControl.Break:
                {
                    return true;
                }
                default: return false;
            }
        }

        private void InsertBrInstructionForConjoinedBasicBlocks()
        {
            for (BasicBlockInfo cur = _bbHead.next.next; cur != null; cur = cur.next)
            {
                if (cur.group == cur.prev.group && IsPrevBasicBlockControlFlowNextToThis(cur))
                {
                    cur.prev.instructions.Add(Instruction.Create(OpCodes.Br, cur.FirstInstruction));
                }
            }
        }

        private Dictionary<Instruction, BasicBlockInfo> BuildInstructionToBasicBlockInfoDic()
        {
            var inst2bb = new Dictionary<Instruction, BasicBlockInfo>();
            for (BasicBlockInfo cur = _bbHead.next; cur != null; cur = cur.next)
            {
                foreach (var inst in cur.instructions)
                {
                    inst2bb[inst] = cur;
                }
            }
            return inst2bb;
        }


        private class SwitchMachineCase
        {
            public int index;
            public BasicBlockInfo prepareBlock;
            public BasicBlockInfo targetBlock;
        }

        private class BlockGroup
        {
            public BlockGroup parent;

            public List<Instruction> instructions;

            public List<BlockGroup> subGroups;

            public List<BasicBlockInfo> basicBlocks;

            public Instruction switchMachineEntryInst;
            public Instruction switchMachineInst;
            public List<SwitchMachineCase> switchMachineCases;

            public BlockGroup(List<Instruction> instructions, Dictionary<Instruction, BlockGroup> inst2group)
            {
                this.instructions = instructions;
                UpdateInstructionGroup(inst2group);
            }

            public BlockGroup(BlockGroup parent, List<Instruction> instructions, Dictionary<Instruction, BlockGroup> inst2group)
            {
                this.instructions = instructions;
                UpdateInstructionGroup(parent, inst2group);
            }

            public BlockGroup RootParent => parent == null ? this : parent.RootParent;

            public void SetParent(BlockGroup newParent)
            {
                if (parent != null)
                {
                    Assert.IsTrue(parent != newParent, "Parent group should not be the same as new parent");
                    Assert.IsTrue(parent.subGroups.Contains(this), "Parent group should already contain this group");
                    parent.subGroups.Remove(this);
                }
                parent = newParent;
                if (newParent.subGroups == null)
                {
                    newParent.subGroups = new List<BlockGroup>();
                }
                Assert.IsFalse(newParent.subGroups.Contains(this), "New parent group should not already contain this group");
                newParent.subGroups.Add(this);
            }

            private void UpdateInstructionGroup(Dictionary<Instruction, BlockGroup> inst2group)
            {
                foreach (var inst in instructions)
                {
                    if (inst2group.TryGetValue(inst, out BlockGroup existGroup))
                    {
                        if (this != existGroup)
                        {
                            BlockGroup rootParent = existGroup.RootParent;
                            if (rootParent != this)
                            {
                                rootParent.SetParent(this);
                            }
                        }
                    }
                    else
                    {
                        inst2group[inst] = this;
                    }
                }
            }

            private void UpdateInstructionGroup(BlockGroup parentGroup, Dictionary<Instruction, BlockGroup> inst2group)
            {
                foreach (var inst in instructions)
                {
                    BlockGroup existGroup = inst2group[inst];
                    Assert.AreEqual(parentGroup, existGroup, "Instruction group parent should be the same as parent group");
                    inst2group[inst] = this;
                }
                SetParent(parentGroup);
            }

            public void SplitInstructionsNotInAnySubGroupsToIndividualGroups(Dictionary<Instruction, BlockGroup> inst2group)
            {
                if (subGroups == null || subGroups.Count == 0 || instructions.Count == 0)
                {
                    return;
                }

                foreach (var subGroup in subGroups)
                {
                    subGroup.SplitInstructionsNotInAnySubGroupsToIndividualGroups(inst2group);
                }

                var finalGroupList = new List<BlockGroup>();
                var curGroupInstructions = new List<Instruction>();

                var firstInst2SubGroup = subGroups.ToDictionary(g => g.instructions[0]);
                foreach (var inst in instructions)
                {
                    BlockGroup group = inst2group[inst];
                    if (group == this)
                    {
                        curGroupInstructions.Add(inst);
                    }
                    else
                    {
                        if (curGroupInstructions.Count > 0)
                        {
                            finalGroupList.Add(new BlockGroup(this, curGroupInstructions, inst2group));
                            curGroupInstructions = new List<Instruction>();
                        }
                        if (firstInst2SubGroup.TryGetValue(inst, out var subGroup))
                        {
                            finalGroupList.Add(subGroup);
                        }
                    }
                }
                if (curGroupInstructions.Count > 0)
                {
                    finalGroupList.Add(new BlockGroup(this, curGroupInstructions, inst2group));
                }
                this.subGroups = finalGroupList;
            }

            public void ComputeBasicBlocks(Dictionary<Instruction, BasicBlockInfo> inst2bb, Func<Local> switchIndexLocalGetter)
            {
                if (subGroups == null || subGroups.Count == 0)
                {
                    basicBlocks = new List<BasicBlockInfo>();
                    foreach (var inst in instructions)
                    {
                        BasicBlockInfo block = inst2bb[inst];
                        if (block.group != null)
                        {
                            if (block.group != this)
                            {
                                throw new Exception("BasicBlockInfo group should be the same as this BlockGroup");
                            }
                        }
                        else
                        {
                            block.group = this;
                            basicBlocks.Add(block);
                        }
                    }
                    switchMachineEntryInst = Instruction.Create(OpCodes.Ldloc, switchIndexLocalGetter());
                    switchMachineInst = Instruction.Create(OpCodes.Switch, new List<Instruction>());
                    switchMachineCases = new List<SwitchMachineCase>();
                    return;
                }
                foreach (var subGroup in subGroups)
                {
                    subGroup.ComputeBasicBlocks(inst2bb, switchIndexLocalGetter);
                }
            }
        }

        private class TryBlockGroup : BlockGroup
        {
            public TryBlockGroup(List<Instruction> instructions, Dictionary<Instruction, BlockGroup> inst2group) : base(instructions, inst2group)
            {
            }
        }

        private class ExceptionHandlerGroup : BlockGroup
        {
            public readonly ExceptionHandler exceptionHandler;

            public ExceptionHandlerGroup(ExceptionHandler exceptionHandler, List<Instruction> instructions, Dictionary<Instruction, BlockGroup> inst2group) : base(instructions, inst2group)
            {
                this.exceptionHandler = exceptionHandler;
            }
        }

        private class ExceptionFilterGroup : BlockGroup
        {
            public readonly ExceptionHandler exceptionHandler;

            public ExceptionFilterGroup(ExceptionHandler exceptionHandler, List<Instruction> instructions, Dictionary<Instruction, BlockGroup> inst2group) : base(instructions, inst2group)
            {
                this.exceptionHandler = exceptionHandler;
            }
        }

        private class ExceptionHandlerWithFilterGroup : BlockGroup
        {
            public readonly ExceptionHandler exceptionHandler;
            public readonly ExceptionFilterGroup filterGroup;
            public readonly ExceptionHandlerGroup handlerGroup;
            public ExceptionHandlerWithFilterGroup(ExceptionHandler exceptionHandler, ExceptionFilterGroup filterGroup, ExceptionHandlerGroup handlerGroup, List<Instruction> instructions, Dictionary<Instruction, BlockGroup> inst2group) : base(instructions, inst2group)
            {
                this.exceptionHandler = exceptionHandler;
                this.filterGroup = filterGroup;
                this.handlerGroup = handlerGroup;
            }
        }

        class TryBlockInfo
        {
            public Instruction tryStart;
            public Instruction tryEnd;
            public TryBlockGroup blockGroup;
        }

        private Dictionary<Instruction, int> BuildInstruction2Index()
        {
            IList<Instruction> instructions = _method.Body.Instructions;
            var inst2Index = new Dictionary<Instruction, int>(instructions.Count);
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                inst2Index.Add(inst, i);
            }
            return inst2Index;
        }

        private BlockGroup SplitBasicBlockGroup()
        {
            Dictionary<Instruction, int> inst2Index = BuildInstruction2Index();
            var inst2blockGroup = new Dictionary<Instruction, BlockGroup>();

            List<Instruction> instructions = (List<Instruction>)_method.Body.Instructions;

            var tryBlocks = new List<TryBlockInfo>();
            foreach (var ex in _method.Body.ExceptionHandlers)
            {
                TryBlockInfo tryBlock = tryBlocks.Find(block => block.tryStart == ex.TryStart && block.tryEnd == ex.TryEnd);
                if (tryBlock == null)
                {
                    int startIndex = inst2Index[ex.TryStart];
                    int endIndex = ex.TryEnd != null ? inst2Index[ex.TryEnd] : inst2Index.Count;
                    TryBlockGroup blockGroup = new TryBlockGroup(instructions.GetRange(startIndex, endIndex - startIndex), inst2blockGroup);
                    tryBlock = new TryBlockInfo
                    {
                        tryStart = ex.TryStart,
                        tryEnd = ex.TryEnd,
                        blockGroup = blockGroup,
                    };
                    tryBlocks.Add(tryBlock);
                }
                if (ex.FilterStart != null)
                {
                    int filterStartIndex = inst2Index[ex.FilterStart];
                    int filterEndIndex = ex.HandlerStart != null ? inst2Index[ex.HandlerStart] : inst2Index.Count;
                    int handlerStartIndex = filterEndIndex;
                    int handlerEndIndex = ex.HandlerEnd != null ? inst2Index[ex.HandlerEnd] : inst2Index.Count;

                    var filterGroup = new ExceptionFilterGroup(ex, instructions.GetRange(filterStartIndex, filterEndIndex - filterStartIndex), inst2blockGroup);
                    var handlerGroup = new ExceptionHandlerGroup(ex, instructions.GetRange(handlerStartIndex, handlerEndIndex - handlerStartIndex), inst2blockGroup);
                    var filterHandlerGroup = new ExceptionHandlerWithFilterGroup(ex, filterGroup, handlerGroup,
                        instructions.GetRange(filterStartIndex, handlerEndIndex - filterStartIndex), inst2blockGroup);
                }
                else
                {
                    int handlerStartIndex = inst2Index[ex.HandlerStart];
                    int handlerEndIndex = ex.HandlerEnd != null ? inst2Index[ex.HandlerEnd] : inst2Index.Count;
                    ExceptionHandlerGroup handlerGroup = new ExceptionHandlerGroup(ex, instructions.GetRange(handlerStartIndex, handlerEndIndex - handlerStartIndex), inst2blockGroup);
                }
            }
            var rootGroup = new BlockGroup(new List<Instruction>(instructions), inst2blockGroup);
            rootGroup.SplitInstructionsNotInAnySubGroupsToIndividualGroups(inst2blockGroup);

            rootGroup.ComputeBasicBlocks(BuildInstructionToBasicBlockInfoDic(), () => GlobalSwitchIndexLocal);
            return rootGroup;
        }

        private void FixInstructionTargets()
        {
            var inst2bb = BuildInstructionToBasicBlockInfoDic();
            foreach (var ex in _method.Body.ExceptionHandlers)
            {
                if (ex.TryStart != null)
                {
                    ex.TryStart = inst2bb[ex.TryStart].GroupFirstInstruction;
                }
                if (ex.TryEnd != null)
                {
                    ex.TryEnd = inst2bb[ex.TryEnd].GroupFirstInstruction;
                }
                if (ex.HandlerStart != null)
                {
                    ex.HandlerStart = inst2bb[ex.HandlerStart].GroupFirstInstruction;
                }
                if (ex.HandlerEnd != null)
                {
                    ex.HandlerEnd = inst2bb[ex.HandlerEnd].GroupFirstInstruction;
                }
                if (ex.FilterStart != null)
                {
                    ex.FilterStart = inst2bb[ex.FilterStart].GroupFirstInstruction;
                }
            }
            //foreach (var inst in inst2bb.Keys)
            //{
            //    if (inst.Operand is Instruction targetInst)
            //    {
            //        inst.Operand = inst2bb[targetInst].FirstInstruction;
            //    }
            //    else if (inst.Operand is Instruction[] targetInsts)
            //    {
            //        for (int i = 0; i < targetInsts.Length; i++)
            //        {
            //            targetInsts[i] = inst2bb[targetInsts[i]].FirstInstruction;
            //        }
            //    }
            //}
        }

        private void BuildInstructions()
        {
            IList<Instruction> methodInstructions = _method.Body.Instructions;
            methodInstructions.Clear();
            for (BasicBlockInfo cur = _bbHead.next; cur != null; cur = cur.next)
            {
                foreach (Instruction inst in cur.instructions)
                {
                    methodInstructions.Add(inst);
                }
            }
            _method.Body.InitLocals = true;
            //_method.Body.MaxStack = Math.Max(_method.Body.MaxStack , (ushort)1); // TODO: set to a reasonable value
            //_method.Body.KeepOldMaxStack = true;
            //_method.Body.UpdateInstructionOffsets();
        }

        public bool TryObfus()
        {
            // TODO: TEMP
            //if (_method.Body.HasExceptionHandlers)
            //{
            //    return false;
            //}
            if (_method.HasGenericParameters || _method.DeclaringType.HasGenericParameters)
            {
                return false;
            }
            var evc = new EvalStackCalculator(_method);
            BuildBasicBlockLink(evc);
            if (!CheckNotContainsNotSupportedEvalStackData())
            {
                Debug.LogError($"Method {_method.FullName} contains unsupported EvalStackData, obfuscation skipped.");
                return false;
            }
            BlockGroup rootGroup = SplitBasicBlockGroup();
            if (rootGroup.basicBlocks != null && rootGroup.basicBlocks.Count == 1)
            {
                return false;
            }
            ComputeInputOutputArguments();
            AdjustInputOutputEvalStack();
            InsertBrInstructionForConjoinedBasicBlocks();
            InsertSwitchMachineBasicBlockForGroups(rootGroup);

            FixInstructionTargets();
            BuildInstructions();
            return true;
        }
    }
}
