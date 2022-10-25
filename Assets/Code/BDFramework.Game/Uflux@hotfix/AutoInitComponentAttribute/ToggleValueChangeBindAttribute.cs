using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class ToggleValueChangeBindAttribute : AutoInitComponentAttribute
    {
        private readonly string path;
        
        public ToggleValueChangeBindAttribute(string path)
        {
            this.path = path;
        }

        public override void AutoSetMethod(IComponent com, MethodInfo methodInfo)
        {
            var toggle = com.Transform.Find(this.path)?.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    methodInfo.Invoke(com,new object[]{isOn});
                });
            }
            else
            {
                Debug.LogError($"绑定Toggle：{this.path}错误");
            }
        }
    }
}