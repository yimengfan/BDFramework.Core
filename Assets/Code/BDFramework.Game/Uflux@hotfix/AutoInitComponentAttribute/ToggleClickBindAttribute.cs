using System;
using System.Reflection;
// using ILRuntime.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class ToggleClickBindAttribute : AutoAssignAttribute
    {
        private readonly string path;
        private bool initialized;
        private Action<bool> cacheAction;
        
        public ToggleClickBindAttribute(string path)
        {
            this.path = path;
        }
        
        private void AddEvent(IComponent com, FieldInfo fieldInfo, bool isOn)
        {
            if (!initialized)
            {
                // if (fieldInfo is ILRuntimeFieldInfo runtimeFieldInfo)
                // {
                //     cacheAction = (Action<bool>) runtimeFieldInfo.GetValue(com);
                // }
                // else
                {  
                    cacheAction = (Action<bool>) fieldInfo.GetValue(com);
                }

                initialized = true;
            }
            cacheAction?.Invoke(isOn);
        }
        
        /// <summary>
        /// 设置字段
        /// </summary>
        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            var button = com.Transform.Find(this.path).GetComponent<Toggle>();
            if (button != null)
            {
                var unityEvent = button.onValueChanged;
                Action<bool> action = null;
                unityEvent.AddListener((isOn) =>
                {
                    // 打乱
                    if (action == null)
                    {
                        // 区分热更延迟添加监听事件
                        // if (fieldInfo is ILRuntimeFieldInfo runtimeFieldInfo)
                        // {
                        //     action = (Action<bool>) runtimeFieldInfo.GetValue(com);
                        // }
                        // else
                        {  
                            action = (Action<bool>) fieldInfo.GetValue(com);
                        }
                    }

                    action?.Invoke(isOn);
                });
            }
            else
            {
                Debug.LogError($"绑定Button:{this.path}错误!");
            }
        }
    }
}