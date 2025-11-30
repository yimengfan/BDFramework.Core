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

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Settings;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.FieldEncrypt
{

    public class FieldEncryptPass : InstructionObfuscationPassBase
    {
        private FieldEncryptionSettingsFacade _settings;
        private IEncryptPolicy _encryptionPolicy;
        private IFieldEncryptor _memoryEncryptor;

        public override ObfuscationPassType Type => ObfuscationPassType.FieldEncrypt;

        public FieldEncryptPass(FieldEncryptionSettingsFacade settings)
        {
            _settings = settings;
        }

        protected override bool ForceProcessAllAssembliesAndIgnoreAllPolicy => true;

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _memoryEncryptor = new DefaultFieldEncryptor(ctx.moduleEntityManager, _settings);
            _encryptionPolicy = new ConfigurableEncryptPolicy(ctx.obfuzIgnoreScopeComputeCache, ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        private bool IsSupportedFieldType(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.I4:
                case ElementType.I8:
                case ElementType.U4:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                return true;
                default: return false;
            }
        }

        protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            Code code = inst.OpCode.Code;
            if (!(inst.Operand is IField field) || !field.IsField)
            {
                return false;
            }
            FieldDef fieldDef = field.ResolveFieldDefThrow();
            if (!IsSupportedFieldType(fieldDef.FieldSig.Type) || !_encryptionPolicy.NeedEncrypt(fieldDef))
            {
                return false;
            }
            switch (code)
            {
                case Code.Ldfld:
                {
                    _memoryEncryptor.Decrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Stfld:
                {
                    _memoryEncryptor.Encrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Ldsfld:
                {
                    _memoryEncryptor.Decrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Stsfld:
                {
                    _memoryEncryptor.Encrypt(callingMethod, fieldDef, outputInstructions, inst);
                    break;
                }
                case Code.Ldflda:
                case Code.Ldsflda:
                {
                    throw new System.Exception($"You shouldn't get reference to memory encryption field: {field}");
                }
                default: return false;
            }
            //Debug.Log($"memory encrypt field: {field}");
            return true;
        }
    }
}
