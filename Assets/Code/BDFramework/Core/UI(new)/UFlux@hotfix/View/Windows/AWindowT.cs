using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.View.Props;
using ILRuntime.Runtime;
using LitJson;
using UnityEngine;
using Object = System.Object;

namespace BDFramework.UFlux
{
    /// <summary>
    /// Window基类
    /// 不带Flux Store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AWindow<T> : Component<T>, IWindow, IUIMessage where T : PropsBase, new()
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

        //
        public delegate void UIMessageDelegate(UIMessageData message);

        /// <summary>
        /// Action 回调表
        /// </summary>
        protected Dictionary<int, MethodInfo> callbackMap = new Dictionary<int, MethodInfo>();

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

                    callbackMap[_attr.MessageName] = methodInfo;
                    //很惨，以下 热更中用不了~
//                    var action = Delegate.CreateDelegate(typeof(UIMessageDelegate), this, methodInfo) as UIMessageDelegate;
//                    if (action != null)
//                    {
//                      
//                        callbackMap[_attr.MessageName] = action;
//                    }
//                    else
//                    {
//                        BDebug.LogError("uimessage 函数签名错误:" + methodInfo.Name);
//                    }
                }
            }
        }


        /// <summary>
        /// 更新UI使用的数据
        /// </summary>
        /// <param name="uiMsg">数据</param>
        public void SendMessage(UIMessageData uiMsg)
        {
          
            //所有的消息会被派发给子窗口
//            var keys = subWindowsMap.Keys.ToList();
//            for (int i = 0; i < keys.Count; i++)
//            {
//                var k = keys[i];
//                subWindowsMap[k].SendMessage(uiMsg);
//            }

            foreach (var subWin in subWindowsMap.Values)
            {
                subWin.SendMessage(uiMsg);
            }
            //TODO: 执行完Invoke会导致 map的堆栈出问题，
            MethodInfo method = null;
            var        key    = uiMsg.Name.GetHashCode();
            bool       flag   = this.callbackMap.TryGetValue(key, out method);
            if (flag)
            {
                method.Invoke(this, new object[] {uiMsg});
            }
        }

        #endregion

        #region 子窗口

        protected Dictionary<int, IWindow> subWindowsMap = new Dictionary<int, IWindow>();

        /// <summary>
        /// 注册窗口
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="win"></param>
        protected void RegisterSubWindow(int index, IWindow win)
        {
            subWindowsMap[index] = win;
        }

        /// <summary>
        /// 获取窗口
        /// </summary>
        /// <param name="enum"></param>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public T1 GetSubWindow<T1>(int index)
        {
            IWindow win = null;
            subWindowsMap.TryGetValue(index, out win);
            return (T1) win;
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