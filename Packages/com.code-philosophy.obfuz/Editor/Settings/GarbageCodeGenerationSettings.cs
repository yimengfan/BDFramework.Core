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

using System;

namespace Obfuz.Settings
{
    public enum GarbageCodeType
    {
        None,
        Config,
        UI,
    }

    [Serializable]
    public class GarbageCodeGenerationTask
    {
        public int codeGenerationRandomSeed;

        public string classNamespace = "__GarbageCode";

        public string classNamePrefix = "__GeneratedGarbageClass";

        public int classCount = 100;

        public int methodCountPerClass = 10;

        public int fieldCountPerClass = 50;

        public GarbageCodeType garbageCodeType = GarbageCodeType.Config;

        public string outputPath = "Assets/Obfuz/GarbageCode";
    }

    [Serializable]
    public class GarbageCodeGenerationSettings
    {
        public string codeGenerationSecret = "Garbage Code";

        public GarbageCodeGenerationTask defaultTask;

        public GarbageCodeGenerationTask[] additionalTasks;
    }
}
