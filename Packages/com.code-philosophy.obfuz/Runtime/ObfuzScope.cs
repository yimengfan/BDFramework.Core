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

﻿using System;

namespace Obfuz
{
    [Flags]
    public enum ObfuzScope
    {
        None = 0x0,
        TypeName = 0x1,
        Field = 0x2,
        MethodName = 0x4,
        MethodParameter = 0x8,
        MethodBody = 0x10,
        Method = MethodName | MethodParameter | MethodBody,
        PropertyName = 0x20,
        PropertyGetterSetterName = 0x40,
        Property = PropertyName | PropertyGetterSetterName,
        EventName = 0x100,
        EventAddRemoveFireName = 0x200,
        Event = EventName | PropertyGetterSetterName,
        Module = 0x1000,
        All = TypeName | Field | Method | Property | Event,
    }
}
