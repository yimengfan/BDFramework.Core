using System;
using System.Reflection;
using ILRuntime.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class ButtonClickBindAttribute : AutoInitComponentAttribute
    {
        private readonly string path;
        
        public ButtonClickBindAttribute(string path)
        {
            this.path = path;
        }
        
        /// <summary>
        /// 设置字段
        /// </summary>
        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            var button = com.Transform.Find(this.path).GetComponent<Button>();
            if (button != null)
            {
                var unityEvent = button.onClick;
                Action action = null;
                unityEvent.AddListener(() =>
                {
                    // 打乱
                    if (action == null)
                    {
                        // 区分热更延迟添加监听事件
                        if (fieldInfo is ILRuntimeFieldInfo runtimeFieldInfo)
                        {
                            action = (Action) runtimeFieldInfo.GetValue(com);
                        }
                        else
                        {  
                            action = (Action) fieldInfo.GetValue(com);
                        }
                    }

                    action?.Invoke();
                });
            }
            else
            {
                Debug.LogError($"绑定Button:{this.path}错误!");
            }
        }
    }
}