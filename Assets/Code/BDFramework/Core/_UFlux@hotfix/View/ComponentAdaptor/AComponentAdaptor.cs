using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    abstract public class AComponentAdaptor
    {
        protected Dictionary<string, Action< UIBehaviour,object>> setPropActionMap = new Dictionary<string, Action<UIBehaviour,object>>();

        public AComponentAdaptor()
        {
            Init();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init()
        {
            var t = this.GetType();
            var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var ms = t.GetMethods(flag);
            foreach (var methodInfo in ms)
            {
                var attrs = methodInfo.GetCustomAttributes(typeof(ComponentValueAdaptorAttribute), false);
                if (attrs.Length > 0)
                {
                    var attr = attrs[0] as ComponentValueAdaptorAttribute;
                    //注册
                    var action = Delegate.CreateDelegate(typeof(Action<UIBehaviour,object>), this, methodInfo) as Action<UIBehaviour,object>;
                    if (action != null)
                    {
                        setPropActionMap[attr.FieldName] = action;
                    }
                    else
                    {
                        BDebug.Log("com value adaptor 签名错误：" + methodInfo.Name);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="uiBehaviour"></param>
        /// <param name="propName"></param>
        /// <param name="propValue"></param>
        public virtual void SetData(UIBehaviour uiBehaviour, string propName, object propValue)
        {
            Action<UIBehaviour,object> action = null;
            this.setPropActionMap.TryGetValue(propName, out action);
            if (action != null)
            {
                action(uiBehaviour,propValue);
            }
            else
            {
                BDebug.LogError("不存在赋值字段:" + propName);
            }
        }

        /// <summary>
        /// enable
        /// </summary>
        /// <param name="propValue"></param>
        [ComponentValueAdaptor(nameof(UIBehaviour.enabled))]
        private void SetProp_Enable(UIBehaviour uiBehaviour,object propValue)
        {
            uiBehaviour.enabled = (bool) propValue;
        }

        /// <summary>
        /// active
        /// </summary>
        /// <param name="propValue"></param>
        [ComponentValueAdaptor(nameof(UIBehaviour.gameObject.active))]
        private void SetProp_Active(UIBehaviour uiBehaviour,object propValue)
        {
            uiBehaviour.gameObject.SetActive((bool) propValue);
        }
    }
}