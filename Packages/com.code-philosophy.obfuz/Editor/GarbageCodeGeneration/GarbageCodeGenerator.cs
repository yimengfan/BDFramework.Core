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

﻿using Obfuz.Settings;
using Obfuz.Utils;
using System;
using UnityEngine;

namespace Obfuz.GarbageCodeGeneration
{

    public class GarbageCodeGenerator
    {
        private const int CodeGenerationSecretKeyLength = 1024;

        private readonly GarbageCodeGenerationSettings _settings;
        private readonly int[] _intGenerationSecretKey;

        public GarbageCodeGenerator(GarbageCodeGenerationSettings settings)
        {
            _settings = settings;

            byte[] byteGenerationSecretKey = KeyGenerator.GenerateKey(settings.codeGenerationSecret, CodeGenerationSecretKeyLength);
            _intGenerationSecretKey = KeyGenerator.ConvertToIntKey(byteGenerationSecretKey);
        }

        public void Generate()
        {
            GenerateTask(_settings.defaultTask);
            if (_settings.additionalTasks != null && _settings.additionalTasks.Length > 0)
            {
                foreach (var task in _settings.additionalTasks)
                {
                    GenerateTask(task);
                }
            }
        }

        public void CleanCodes()
        {
            Debug.Log($"Cleaning generated garbage codes begin.");
            if (_settings.defaultTask != null)
            {
                FileUtil.RemoveDir(_settings.defaultTask.outputPath, true);
            }
            if (_settings.additionalTasks != null && _settings.additionalTasks.Length > 0)
            {
                foreach (var task in _settings.additionalTasks)
                {
                    FileUtil.RemoveDir(task.outputPath, true);
                }
            }
        }

        private void GenerateTask(GarbageCodeGenerationTask task)
        {
            Debug.Log($"Generating garbage code with seed: {task.codeGenerationRandomSeed}, class count: {task.classCount}, method count per class: {task.methodCountPerClass}, types: {task.garbageCodeType}, output path: {task.outputPath}");

            if (string.IsNullOrWhiteSpace(task.outputPath))
            {
                throw new Exception("outputPath of GarbageCodeGenerationTask is empty!");
            }

            var generator = CreateSpecificCodeGenerator(task.garbageCodeType);

            var parameters = new GenerationParameters
            {
                random = new RandomWithKey(_intGenerationSecretKey, task.codeGenerationRandomSeed),
                classNamespace = task.classNamespace,
                classNamePrefix = task.classNamePrefix,
                classCount = task.classCount,
                methodCountPerClass = task.methodCountPerClass,
                fieldCountPerClass = task.fieldCountPerClass,
                outputPath = task.outputPath,
            };
            generator.Generate(parameters);

            Debug.Log($"Generate garbage code end.");
        }

        private ISpecificGarbageCodeGenerator CreateSpecificCodeGenerator(GarbageCodeType type)
        {
            switch (type)
            {
                case GarbageCodeType.Config: return new ConfigGarbageCodeGenerator();
                case GarbageCodeType.UI: return new UIGarbageCodeGenerator();
                default: throw new NotSupportedException($"Garbage code type {type} is not supported.");
            }
        }
    }
}
