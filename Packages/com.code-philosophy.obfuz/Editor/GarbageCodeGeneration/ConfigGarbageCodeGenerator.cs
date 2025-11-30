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

﻿using Obfuz.Utils;
using System;
using System.Linq;
using System.Text;

namespace Obfuz.GarbageCodeGeneration
{

    public class ConfigGarbageCodeGenerator : SpecificGarbageCodeGeneratorBase
    {

        private readonly string[] _types = new string[]
        {
            "bool",
            "byte",
            "short",
            "int",
            "long",
            "float",
            "double",
        };

        private string CreateRandomType(IRandom random)
        {
            return _types[random.NextInt(_types.Length)];
        }

        private string GetReadMethodNameOfType(string type)
        {
            switch (type)
            {
                case "bool": return "ReadBoolean";
                case "byte": return "ReadByte";
                case "short": return "ReadInt16";
                case "int": return "ReadInt32";
                case "long": return "ReadInt64";
                case "float": return "ReadSingle";
                case "double": return "ReadDouble";
                default: throw new ArgumentException($"Unsupported type: {type}");
            }
        }
        class FieldGenerationInfo
        {
            public int index;
            public string name;
            public string type;
        }

        class MethodGenerationInfo
        {
            public int index;
            public string name;
        }

        protected override object CreateField(int index, IRandom random, GenerationParameters parameters)
        {
            return new FieldGenerationInfo
            {
                index = index,
                name = $"x{index}",
                type = CreateRandomType(random),
            };
        }

        protected override object CreateMethod(int index, IRandom random, GenerationParameters parameters)
        {
            return new MethodGenerationInfo
            {
                index = index,
                name = $"Load{index}",
            };
        }

        protected override void GenerateUsings(StringBuilder result, IClassGenerationInfo cgi)
        {
        }

        protected override void GenerateField(StringBuilder result, IClassGenerationInfo cgi, IRandom random, object field, string indent)
        {
            var fgi = (FieldGenerationInfo)field;
            result.AppendLine($"{indent}public {fgi.type} {fgi.name};");
        }

        protected override void GenerateMethod(StringBuilder result, IClassGenerationInfo cgi, IRandom random, object method, string indent)
        {
            var mgi = (MethodGenerationInfo)method;
            result.AppendLine($"{indent}public void {mgi.name}(BinaryReader reader)");
            result.AppendLine($"{indent}{{");

            string indent2 = indent + "    ";
            result.AppendLine($"{indent2}int a = 0;");
            result.AppendLine($"{indent2}int b = 0;");
            int maxN = 100;
            var shuffledFields = cgi.Fields.ToList();
            RandomUtil.ShuffleList(shuffledFields, random);
            foreach (FieldGenerationInfo fgi in shuffledFields)
            {
                result.AppendLine($"{indent2}this.{fgi.name} = reader.{GetReadMethodNameOfType(fgi.type)}();");
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}a = b * {random.NextInt(maxN)} + reader.ReadInt32();");
                    result.AppendLine($"{indent2}b = a * reader.ReadInt32() + {random.NextInt(maxN)};");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}a += {random.NextInt(0, 10000)};");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}b += {random.NextInt(0, 10000)};");
                }
            }

            result.AppendLine($"{indent}}}");
        }
    }
}
