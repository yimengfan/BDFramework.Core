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

﻿namespace Obfuz.Utils
{
    public class RandomWithKey : IRandom
    {
        private const long a = 1664525;
        private const long c = 1013904223;
        private const long m = 4294967296; // 2^32

        private readonly int[] _key;

        private int _nextIndex;

        private int _seed;

        public RandomWithKey(int[] key, int seed)
        {
            _key = key;
            _seed = seed;
        }

        public int[] Key => _key;

        public int NextInt(int min, int max)
        {
            return min + NextInt(max - min);
        }

        public int NextInt(int max)
        {
            return (int)((uint)NextInt() % (uint)max);
        }

        private int GetNextSalt()
        {
            if (_nextIndex >= _key.Length)
            {
                _nextIndex = 0;
            }
            return _key[_nextIndex++];
        }

        public int NextInt()
        {
            _seed = (int)((a * _seed + c) % m);
            return _seed ^ GetNextSalt();
        }

        public long NextLong()
        {
            return ((long)NextInt() << 32) | (uint)NextInt();
        }

        public float NextFloat()
        {
            return (float)((double)(uint)NextInt() / uint.MaxValue);
        }

        public bool NextInPercentage(float percentage)
        {
            return NextFloat() < percentage;
        }
    }
}
