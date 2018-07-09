using System;
using Mono.CompilerServices.SymbolWriter;
using UnityEngine;

namespace BDFramework.UI
{
    public class ValueType: Attribute
    {
        public Type Type;

        public ValueType(Type t)
        {
            this.Type = t;
        }
    }
}