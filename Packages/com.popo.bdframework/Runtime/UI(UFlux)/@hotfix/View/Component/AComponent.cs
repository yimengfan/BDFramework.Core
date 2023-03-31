using System;
using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 无属性的Props
    /// TODO 不允许外部使用
    /// </summary>
    public class NoProps : APropsBase
    {
    }

    /// <summary>
    /// Component的基类
    /// </summary>
    abstract public class AComponent : ATComponent<NoProps>
    {
        /// <summary>
        /// 动态传入节点
        /// </summary>
        /// <param name="trans"></param>
        protected AComponent(Transform trans) : base(trans)
        {
        }

        /// <summary>
        /// 可控制是否加载
        /// </summary>
        /// <param name="isLoadAsset"></param>
        /// <param name="resPath"></param>
        protected AComponent(bool isLoadAsset = true) : base(isLoadAsset)
        {
        }

        /// <summary>
        /// 传入资源绑定，不加载
        /// </summary>
        /// <param name="resPath"></param>
        protected AComponent(string resPath) : base(resPath)
        {
        }
    }
}