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

﻿using System.Collections.Generic;
using System.Diagnostics;

namespace Obfuz
{
    public class Pipeline
    {
        private readonly List<IObfuscationPass> _passes = new List<IObfuscationPass>();

        public bool Empty => _passes.Count == 0;

        public Pipeline AddPass(IObfuscationPass pass)
        {
            _passes.Add(pass);
            return this;
        }

        public void Start()
        {
            foreach (var pass in _passes)
            {
                pass.Start();
            }
        }

        public void Stop()
        {

            foreach (var pass in _passes)
            {
                pass.Stop();
            }
        }

        public void Run()
        {
            var sw = new Stopwatch();
            foreach (var pass in _passes)
            {
                sw.Restart();
                pass.Process();
                sw.Stop();
                UnityEngine.Debug.Log($"Pass: {pass.GetType().Name} process cost time: {sw.ElapsedMilliseconds}ms");
            }
        }
    }
}
