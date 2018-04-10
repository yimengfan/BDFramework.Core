using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.CLR.Method;
using UnityEngine;
using ILRuntimeAppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

namespace BDFramework.ScreenView
{
    /// <summary>
    /// 这个类作为热更类的包装类
    /// 1.这个里面包装的就是热更类的对象和再封装，方便外部调用
    /// </summary>
    public class ScreenView_HotFix_Wrapper : IScreenView
    {
        private ILRuntimeAppDomain appdomain;
        private IType type;
        private object ilrobject;
        private Dictionary<string, IMethod> methodMap;

        public ScreenView_HotFix_Wrapper(ILRuntimeAppDomain appdomain, string typename)
        {
            this.appdomain = appdomain;
            this.type = appdomain.LoadedTypes[typename];
            this.ilrobject = ((ILType) type).Instantiate();
            methodMap = new Dictionary<string, IMethod>();
            //注册所有函数
            methodMap["get_name"] = type.GetMethod("get_name", 0);
            methodMap["get_isBusy"] = type.GetMethod("get_isBusy", 0);
            methodMap["set_isBusy"] = type.GetMethod("set_isBusy", 1);
            methodMap["get_isLoad"] = type.GetMethod("get_isLoad", 0);
            methodMap["set_isLoad"] = type.GetMethod("set_isLoad", 1);
            methodMap["get_isTransparent"] = type.GetMethod("get_isTransparent", 0);
            methodMap["set_isTransparent"] = type.GetMethod("set_isTransparent", 1);
            //
            methodMap["BeginExit"] = type.GetMethod("BeginExit", 1);
            methodMap["BeginInit"] = type.GetMethod("BeginInit", 2);
            methodMap["Destory"] = type.GetMethod("Destory", 0);
            methodMap["Update"] = type.GetMethod("Update", 1);
            methodMap["UpdateTask"] = type.GetMethod("UpdateTask", 1);
        }

        public bool IsBusy
        {
            get { return (bool) appdomain.Invoke(methodMap["get_isBusy"], this.ilrobject, null); }
            set { appdomain.Invoke(methodMap["set_isBusy"], this.ilrobject, new object[] {value}); }
        }

        public bool IsLoad
        {
            get { return (bool) appdomain.Invoke(methodMap["get_isLoad"], this.ilrobject, null); }
            set { appdomain.Invoke(methodMap["set_isLoad"], this.ilrobject, new object[] {value}); }
        }

        public bool IsTransparent
        {
            get { return (bool) appdomain.Invoke(methodMap["get_isTransparent"], this.ilrobject, null); }
            set { appdomain.Invoke(methodMap["set_isTransparent"], this.ilrobject, new object[] {value}); }
        }

        public string Name
        {
            get { return (string) appdomain.Invoke(methodMap["get_name"], this.ilrobject, null); }
            set { appdomain.Invoke(methodMap["set_name"], this.ilrobject, new object[] {value}); }
        }

        public void BeginExit(Action<Exception> onExit)
        {
            appdomain.Invoke(methodMap["BeginExit"], this.ilrobject, new object[] {onExit});
        }

        public void BeginInit(Action<Exception> onInit, ScreenViewLayer layer)
        {
            appdomain.Invoke(methodMap["BeginInit"], this.ilrobject, new object[] {onInit, layer});
        }

        public void Destory()
        {
            appdomain.Invoke(methodMap["Destory"], this.ilrobject, null);
        }

        public void Update(float delta)
        {
            appdomain.Invoke(methodMap["Update"], this.ilrobject, new object[] {delta});
        }

        public void UpdateTask(float delta)
        {
            appdomain.Invoke(methodMap["UpdateTask"], this.ilrobject, new object[] {delta});
        }

        public void FixedUpdate(float delta)
        {
        }
    }
}