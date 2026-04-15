using System;
// using ILRuntime.CLR.TypeSystem;
// using ILRuntime.Mono.Cecil;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 组件绑定属性
    /// </summary>
    public class ComponentValueBindAttribute : Attribute
    {
        /// <summary>
        /// 节点路径
        /// </summary>
        public string TransformPath { get; private set; }
        /// <summary>
        /// 绑定UI类type，或者自定义逻辑type
        /// </summary>
        public Type Type;
        /// <summary>
        /// 方法名，
        /// 用以修改 Tranform或者ui值的方法
        /// </summary>
        public string FunctionName { get; private set; }
        
        /// <summary>
        /// 组件
        /// </summary>
        public UIBehaviour UIBehaviour { get; private set; }
        /// <summary>
        /// 节点
        /// </summary>
        public Transform Transform { get; private set; }
        /// <summary>
        /// 构造函数
        /// 热更Attr不支持基础类型以外
        /// </summary>
        /// <param name="uiType"></param>
        /// <param name="functionName"></param>
        public ComponentValueBindAttribute(string transformPath, Type uiType, string functionName)
        {
            this.FunctionName = functionName;
            this.TransformPath = transformPath;
            //这里故意让破坏优化 ilrbug
            //var ot = (object) uiType;
            string typeFullname = "";
            // if (ot is TypeReference)
            // {
            //     typeFullname = ((TypeReference) ot).FullName;
            // }
            // else
            {
                typeFullname = uiType.FullName;
            }
            

            var type = ComponentBindAdaptorManager.Inst.GetBindComponentType(typeFullname);
            this.Type = type;
            if (type == null)
            {
                BDebug.LogError("ComponentBindAdaptor不存在:" +  typeFullname);
            }
        }
    }
}