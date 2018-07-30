using System;
using UnityEngine;

namespace BDFramework.UI
{
    public class UIComponentType: Attribute
    {
        public string ComponentName { get; private set; }

        public UIComponentType(string typefullName)
        {
            this.ComponentName = typefullName;
        }
    }
}