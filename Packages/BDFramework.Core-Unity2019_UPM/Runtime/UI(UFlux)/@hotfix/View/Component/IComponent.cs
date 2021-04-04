using System;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 组件接口
    /// </summary>
    public interface IComponent
    {
        Transform Transform { get; }

        /// <summary>
        /// 是否加载
        /// </summary>
        bool IsLoad { get; }

        /// <summary>
        /// 是否打开
        /// </summary>
        bool IsOpen { get; }

        bool IsDestroy { get; }

        /// <summary>
        /// 同步加载
        /// </summary>
        void Load();

        /// <summary>
        /// 异步加载
        /// </summary>
        void AsyncLoad(Action callback);

        /// <summary>
        /// 初始化
        /// </summary>
        void Init();

        /// <summary>
        /// 打开
        /// </summary>
        void Open();

        /// <summary>
        /// 关闭
        /// </summary>
        void Close();

        /// <summary>
        /// 帧更新
        /// </summary>
        void Update();

        /// <summary>
        /// 删除
        /// </summary>
        void Destroy();

        /// <summary>
        /// 设置prop
        /// </summary>
        /// <param name="propsBase"></param>
        void SetProps(PropsBase propsBase);
    }
}