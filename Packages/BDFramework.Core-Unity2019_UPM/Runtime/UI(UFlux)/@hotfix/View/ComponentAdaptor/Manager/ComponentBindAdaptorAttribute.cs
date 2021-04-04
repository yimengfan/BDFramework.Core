using System;
using BDFramework.Mgr;
using ILRuntime.CLR.TypeSystem;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 组件绑定的处理逻辑
    /// </summary>
    public class ComponentBindAdaptorAttribute : ManagerAttribute
    {
        /// <summary>
        /// 绑定组件的类型
        /// </summary>
        public Type BindComponentType { get; private set; }

        public ComponentBindAdaptorAttribute(string bindComponentTypeName) : base(bindComponentTypeName)
        {
            //1.组件类绑定
            Type type;
            if (ILRuntimeHelper.UIComponentTypes.TryGetValue(bindComponentTypeName, out type))
            {
                this.BindComponentType = type;
                return;
            }

            //2.自定义逻辑类绑定
            //限制typename的命名空间,增加查询速度
            if (ILRuntimeHelper.AppDomain != null && ILRuntimeHelper.AppDomain.LoadedTypes != null) //这两个判断防止编辑器下报错
            {
                var fullname = "BDFramework.UFlux." + bindComponentTypeName;
                IType ilrtype;
                if (ILRuntimeHelper.AppDomain.LoadedTypes.TryGetValue(fullname, out ilrtype))
                {
                    this.BindComponentType = ilrtype.ReflectionType;
                }
                else
                {
                    foreach (var key in ILRuntimeHelper.AppDomain.LoadedTypes.Keys)
                    {
                        if (key.Contains(bindComponentTypeName))
                        {
                            BDebug.LogError("错误命名空间:" + key);
                        }
                    }
                    BDebug.LogError("【UFlux】请检查BindAdaptor命名空间,是否为:" + fullname);
                }
            }
        }
    }
}