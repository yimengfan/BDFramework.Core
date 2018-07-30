using System;
using UnityEngine;

namespace BDFramework.UI
{
    public enum UIComponentEnum 
    {
        Image,
        Text,
        Toggle,
        Slider,
        ScrollBar,
    }
    public class UIComponentType: Attribute
    {
        public string ComponentName { get; private set; }

        public UIComponentType(UIComponentEnum componentEnum)
        {
            this.ComponentName = componentEnum.ToString();
        }
    }
}