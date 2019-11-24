using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.View.Props;
using ILRuntime.Runtime;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// Window基类
    /// 不带Flux Store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AWindow<T> : Component<T>, IWindow ,IUIMessage where T : PropsBase, new()
    {
        public AWindow(string path) : base(path)
        {
            RegisterActions();
        }

        public AWindow(Transform transform) : base(transform)
        {
            RegisterActions();
        }


        /// <summary>
        /// 获取Props
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <exception cref="NotImplementedException"></exception>
        public T1 GetProps<T1>() where T1 : PropsBase, new()
        {
            return this.Props as T1;
        }

        #region UIMessage 

        /// <summary>
        /// Action 回调表
        /// </summary>
        protected Dictionary<int, Action<UIMessageData>> callbackMap = new Dictionary<int, Action<UIMessageData>>();

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
                    var action = Delegate.CreateDelegate(typeof(Action<UIMessageData>), this, methodInfo) as Action<UIMessageData>;
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
        /// <param name="messageData">数据</param>
        public void SendMessage(UIMessageData messageData)
        {
            Action<UIMessageData> action = null;
            var key = messageData.Name.GetHashCode();
            callbackMap.TryGetValue(key, out action);
            if (action != null)
            {
                action(messageData);
            }
            //所有的消息会被派发给子窗口
            if (subWindowsMap.Count > 0)
            {
                foreach (var value in subWindowsMap.Values)
                {
                    var uimassage = value as IUIMessage;
                    if (uimassage != null)
                    {
                        uimassage.SendMessage(messageData);
                    }
                }
            }
            
        }

        #endregion

        #region 子窗口

        protected Dictionary<Enum, IWindow> subWindowsMap = new Dictionary<Enum, IWindow>();

        /// <summary>
        /// 注册窗口
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="win"></param>
        protected void RegisterSubWindow(Enum @enum, IWindow win)
        {
            subWindowsMap[@enum] = win;
        }

        /// <summary>
        /// 获取窗口
        /// </summary>
        /// <param name="enum"></param>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public T1 GetSubWindow<T1>(Enum @enum)
        {
            IWindow win = null;
            subWindowsMap.TryGetValue(@enum, out win);
            return (T1)win;
        }

        /// <summary>
        /// 获取窗口
        /// </summary>
        /// <param name="enum"></param>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public T1 GetSubWindow<T1>() where T1 : class
        {
            foreach (var value in subWindowsMap.Values)
            {
                if (value is T1)
                {
                    return (T1) value;
                }
            }

            return null;
        }
        #endregion
    }
}