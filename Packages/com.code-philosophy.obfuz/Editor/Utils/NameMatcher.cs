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

﻿using System.Text.RegularExpressions;

namespace Obfuz.Utils
{
    public class NameMatcher
    {
        private readonly string _str;
        private readonly Regex _regex;

        public string NameOrPattern => _str;

        public bool IsWildcardPattern => _regex != null;

        public NameMatcher(string nameOrPattern)
        {
            if (string.IsNullOrEmpty(nameOrPattern))
            {
                nameOrPattern = "*";
            }
            _str = nameOrPattern;
            _regex = nameOrPattern.Contains("*") || nameOrPattern.Contains("?") ? new Regex(WildcardToRegex(nameOrPattern)) : null;
        }

        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }

        public bool IsMatch(string name)
        {
            if (_regex != null)
            {
                return _regex.IsMatch(name);
            }
            else
            {
                return _str == name;
            }
        }
    }
}
