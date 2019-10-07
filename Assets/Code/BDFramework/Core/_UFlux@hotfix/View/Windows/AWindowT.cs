using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.View.Props;
using ILRuntime.Runtime;

namespace BDFramework.UFlux
{
    /// <summary>
    /// Window基类
    /// 不带Flux Store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AWindow<T> : Component<T>, IWindow where T : PropsBase, new()
    {
        public AWindow(string path) : base(path)
        {
            RegisterActions();
        }

        #region UIMessage 
        /// <summary>
        /// Action 回调表
        /// </summary>
        protected Dictionary<int, Action<UIMessage>> callbackMap = new Dictionary<int, Action<UIMessage>>();

        /// <summary>
        /// 注册回调
        /// </summary>
        private void RegisterActions()
        {
            //注册回调
            var t = this.GetType();
            var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var methodInfo in t.GetMethods(flag))
            {
                var attrs = methodInfo.GetCustomAttributes(typeof(UIMessageAttribute), false);
                if (attrs.Length > 0)
                {
                    var _attr = attrs[0] as UIMessageAttribute;
                    var action = Delegate.CreateDelegate(typeof(Action<UIMessage>), this, methodInfo) as Action<UIMessage>;
                    if (action != null)
                    {
                        callbackMap[_attr.MessageName] = action;
                    }
                    else
                    {
                        BDebug.LogError("uimessage 函数签名错误:" + methodInfo.Name);
                    }
                }
            }
        }

        /// <summary>
        /// 更新UI使用的数据
        /// </summary>
        /// <param name="message">数据</param>
        public void SendMessage(UIMessage message)
        {
            Action<UIMessage> action = null;
            var key = message.Name.GetHashCode();
            callbackMap.TryGetValue(key, out action);
            if (action != null)
            {
                action(message);
            }
        }

        #endregion
    }
}