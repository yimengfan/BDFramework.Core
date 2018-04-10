using System;
using Mono.CompilerServices.SymbolWriter;
using UnityEngine;

namespace BDFramework.UI
{
    public class BValueType: Attribute
    {
        public Type Type;

        public BValueType(Type t)
        {
            this.Type = t;
        }
    }
}