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
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Obfuz.GarbageCodeGeneration
{
    public abstract class SpecificGarbageCodeGeneratorBase : ISpecificGarbageCodeGenerator
    {
        protected interface IClassGenerationInfo
        {
            string Namespace { get; set; }

            string Name { get; set; }

            IList<object> Fields { get; set; }

            IList<object> Methods { get; set; }
        }

        protected class ClassGenerationInfo : IClassGenerationInfo
        {
            public string Namespace { get; set; }
            public string Name { get; set; }
            public IList<object> Fields { get; set; } = new List<object>();
            public IList<object> Methods { get; set; } = new List<object>();
        }

        public virtual void Generate(GenerationParameters parameters)
        {
            FileUtil.RecreateDir(parameters.outputPath);

            for (int i = 0; i < parameters.classCount; i++)
            {
                Debug.Log($"[{GetType().Name}] Generating class {i}");
                var localRandom = new RandomWithKey(((RandomWithKey)parameters.random).Key, parameters.random.NextInt());
                string outputFile = $"{parameters.outputPath}/__GeneratedGarbageClass_{i}.cs";
                var result = new StringBuilder(64 * 1024);
                GenerateClass(i, localRandom, result, parameters);
                File.WriteAllText(outputFile, result.ToString(), Encoding.UTF8);
                Debug.Log($"[{GetType().Name}] Generated class {i} to {outputFile}");
            }
        }

        protected abstract object CreateField(int index, IRandom random, GenerationParameters parameters);

        protected abstract object CreateMethod(int index, IRandom random, GenerationParameters parameters);

        protected virtual IClassGenerationInfo CreateClassGenerationInfo(string classNamespace, string className, IRandom random, GenerationParameters parameters)
        {
            var cgi = new ClassGenerationInfo
            {
                Namespace = classNamespace,
                Name = className,
            };

            for (int i = 0; i < parameters.fieldCountPerClass; i++)
            {
                cgi.Fields.Add(CreateField(i, random, parameters));
            }

            for (int i = 0; i < parameters.methodCountPerClass; i++)
            {
                cgi.Methods.Add(CreateMethod(i, random, parameters));
            }

            return cgi;
        }

        protected virtual void GenerateClass(int classIndex, IRandom random, StringBuilder result, GenerationParameters parameters)
        {
            IClassGenerationInfo cgi = CreateClassGenerationInfo(parameters.classNamespace, $"{parameters.classNamePrefix}{classIndex}", random, parameters);
            result.AppendLine("using System;");
            result.AppendLine("using System.Collections.Generic;");
            result.AppendLine("using System.Linq;");
            result.AppendLine("using System.IO;");
            result.AppendLine("using UnityEngine;");

            GenerateUsings(result, cgi);

            result.AppendLine($"namespace {cgi.Namespace}");
            result.AppendLine("{");
            result.AppendLine($"    public class {cgi.Name}");
            result.AppendLine("    {");

            string indent = "        ";
            foreach (object field in cgi.Fields)
            {
                GenerateField(result, cgi, random, field, indent);
            }
            foreach (object method in cgi.Methods)
            {
                GenerateMethod(result, cgi, random, method, indent);
            }
            result.AppendLine("    }");
            result.AppendLine("}");
        }

        protected abstract void GenerateUsings(StringBuilder result, IClassGenerationInfo cgi);

        protected abstract void GenerateField(StringBuilder result, IClassGenerationInfo cgi, IRandom random, object field, string indent);

        protected abstract void GenerateMethod(StringBuilder result, IClassGenerationInfo cgi, IRandom random, object method, string indent);
    }
}
