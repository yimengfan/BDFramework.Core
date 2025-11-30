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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.Emit
{
    public class BasicBlock
    {
        public readonly List<Instruction> instructions = new List<Instruction>();

        public readonly List<BasicBlock> inBlocks = new List<BasicBlock>();

        public readonly List<BasicBlock> outBlocks = new List<BasicBlock>();

        public bool inLoop;

        public void AddTargetBasicBlock(BasicBlock target)
        {
            if (!outBlocks.Contains(target))
            {
                outBlocks.Add(target);
            }
            if (!target.inBlocks.Contains(this))
            {
                target.inBlocks.Add(this);
            }
        }
    }

    public class BasicBlockCollection
    {
        private readonly MethodDef _method;

        private readonly List<BasicBlock> _blocks = new List<BasicBlock>();
        private readonly Dictionary<Instruction, BasicBlock> _inst2BlockMap = new Dictionary<Instruction, BasicBlock>();

        public IList<BasicBlock> Blocks => _blocks;

        public BasicBlockCollection(MethodDef method, bool computeInLoop)
        {
            _method = method;
            HashSet<Instruction> splitPoints = BuildSplitPoint(method);
            BuildBasicBlocks(method, splitPoints);
            BuildInOutGraph(method);
            if (computeInLoop)
            {
                ComputeBlocksInLoop();
            }
        }

        public void ComputeBlocksInLoop()
        {
            var loopBlocks = FindLoopBlocks(_blocks);
            foreach (var block in loopBlocks)
            {
                block.inLoop = true;
            }
        }

        public BasicBlock GetBasicBlockByInstruction(Instruction inst)
        {
            return _inst2BlockMap[inst];
        }

        private HashSet<Instruction> BuildSplitPoint(MethodDef method)
        {
            var insts = method.Body.Instructions;
            var splitPoints = new HashSet<Instruction>();
            foreach (ExceptionHandler eh in method.Body.ExceptionHandlers)
            {
                if (eh.TryStart != null)
                {
                    splitPoints.Add(eh.TryStart);
                }
                if (eh.TryEnd != null)
                {
                    splitPoints.Add(eh.TryEnd);
                }
                if (eh.HandlerStart != null)
                {
                    splitPoints.Add(eh.HandlerStart);
                }
                if (eh.HandlerEnd != null)
                {
                    splitPoints.Add(eh.HandlerEnd);
                }
                if (eh.FilterStart != null)
                {
                    splitPoints.Add(eh.FilterStart);
                }
            }

            for (int i = 0, n = insts.Count; i < n; i++)
            {
                Instruction curInst = insts[i];
                Instruction nextInst = i + 1 < n ? insts[i + 1] : null;
                switch (curInst.OpCode.FlowControl)
                {
                    case FlowControl.Branch:
                    {
                        if (nextInst != null)
                        {
                            splitPoints.Add(nextInst);
                        }
                        splitPoints.Add((Instruction)curInst.Operand);
                        break;
                    }
                    case FlowControl.Cond_Branch:
                    {
                        if (nextInst != null)
                        {
                            splitPoints.Add(nextInst);
                        }
                        if (curInst.Operand is Instruction targetInst)
                        {
                            splitPoints.Add(targetInst);
                        }
                        else if (curInst.Operand is Instruction[] targetInsts)
                        {
                            foreach (var target in targetInsts)
                            {
                                splitPoints.Add(target);
                            }
                        }
                        break;
                    }
                    case FlowControl.Return:
                    {
                        if (nextInst != null)
                        {
                            splitPoints.Add(nextInst);
                        }
                        break;
                    }
                    case FlowControl.Throw:
                    {
                        if (nextInst != null)
                        {
                            splitPoints.Add(nextInst);
                        }
                        break;
                    }
                }
            }
            return splitPoints;
        }


        private void BuildBasicBlocks(MethodDef method, HashSet<Instruction> splitPoints)
        {
            var insts = method.Body.Instructions;


            BasicBlock curBlock = new BasicBlock();
            foreach (Instruction inst in insts)
            {
                if (splitPoints.Contains(inst) && curBlock.instructions.Count > 0)
                {
                    _blocks.Add(curBlock);
                    curBlock = new BasicBlock();
                }
                curBlock.instructions.Add(inst);
                _inst2BlockMap.Add(inst, curBlock);
            }
            if (curBlock.instructions.Count > 0)
            {
                _blocks.Add(curBlock);
            }
        }

        private void BuildInOutGraph(MethodDef method)
        {
            var insts = method.Body.Instructions;
            for (int i = 0, n = _blocks.Count; i < n; i++)
            {
                BasicBlock curBlock = _blocks[i];
                BasicBlock nextBlock = i + 1 < n ? _blocks[i + 1] : null;
                Instruction lastInst = curBlock.instructions.Last();
                switch (lastInst.OpCode.FlowControl)
                {
                    case FlowControl.Branch:
                    {
                        Instruction targetInst = (Instruction)lastInst.Operand;
                        BasicBlock targetBlock = GetBasicBlockByInstruction(targetInst);
                        curBlock.AddTargetBasicBlock(targetBlock);
                        break;
                    }
                    case FlowControl.Cond_Branch:
                    {
                        if (lastInst.Operand is Instruction targetInst)
                        {
                            BasicBlock targetBlock = GetBasicBlockByInstruction(targetInst);
                            curBlock.AddTargetBasicBlock(targetBlock);
                        }
                        else if (lastInst.Operand is Instruction[] targetInsts)
                        {
                            foreach (var target in targetInsts)
                            {
                                BasicBlock targetBlock = GetBasicBlockByInstruction(target);
                                curBlock.AddTargetBasicBlock(targetBlock);
                            }
                        }
                        else
                        {
                            throw new Exception("Invalid operand type for conditional branch");
                        }
                        if (nextBlock != null)
                        {
                            curBlock.AddTargetBasicBlock(nextBlock);
                        }
                        break;
                    }
                    case FlowControl.Call:
                    case FlowControl.Next:
                    {
                        if (nextBlock != null)
                        {
                            curBlock.AddTargetBasicBlock(nextBlock);
                        }
                        break;
                    }
                    case FlowControl.Return:
                    case FlowControl.Throw:
                    {
                        break;
                    }
                    default: throw new NotSupportedException($"Unsupported flow control: {lastInst.OpCode.FlowControl} in method {method.FullName}");
                }
            }
        }

        private static HashSet<BasicBlock> FindLoopBlocks(List<BasicBlock> allBlocks)
        {
            // Tarjan算法找强连通分量
            var sccList = FindStronglyConnectedComponents(allBlocks);

            // 筛选有效循环
            var loopBlocks = new HashSet<BasicBlock>();
            foreach (var scc in sccList)
            {
                // 有效循环需满足以下条件之一：
                // 1. 分量包含多个块
                // 2. 单个块有自环（跳转自己）
                if (scc.Count > 1 ||
                    (scc.Count == 1 && scc[0].outBlocks.Contains(scc[0])))
                {
                    foreach (var block in scc)
                    {
                        loopBlocks.Add(block);
                    }
                }
            }
            return loopBlocks;
        }

        private static List<List<BasicBlock>> FindStronglyConnectedComponents(List<BasicBlock> allBlocks)
        {
            int index = 0;
            var stack = new Stack<BasicBlock>();
            var indexes = new Dictionary<BasicBlock, int>();
            var lowLinks = new Dictionary<BasicBlock, int>();
            var onStack = new HashSet<BasicBlock>();
            var sccList = new List<List<BasicBlock>>();

            foreach (var block in allBlocks.Where(b => !indexes.ContainsKey(b)))
            {
                StrongConnect(block);
            }

            return sccList;

            void StrongConnect(BasicBlock v)
            {
                indexes[v] = index;
                lowLinks[v] = index;
                index++;
                stack.Push(v);
                onStack.Add(v);

                foreach (var w in v.outBlocks)
                {
                    if (!indexes.ContainsKey(w))
                    {
                        StrongConnect(w);
                        lowLinks[v] = System.Math.Min(lowLinks[v], lowLinks[w]);
                    }
                    else if (onStack.Contains(w))
                    {
                        lowLinks[v] = System.Math.Min(lowLinks[v], indexes[w]);
                    }
                }

                if (lowLinks[v] == indexes[v])
                {
                    var scc = new List<BasicBlock>();
                    BasicBlock w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w);
                        scc.Add(w);
                    } while (!w.Equals(v));
                    sccList.Add(scc);
                }
            }
        }
    }
}
