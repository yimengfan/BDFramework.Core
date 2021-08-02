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
        void Open(UIMsgData uiMsg=null);

        /// <summary>
        /// 当窗口重新获得焦点时会调用
        /// 如 2覆盖1上面，2关闭，1触发focus
        /// </summary>
        void OnFocus();
    
        /// <summary>
        /// 关闭
        /// </summary>
        void Close();
        
        /// <summary>
        /// 删除
        /// </summary>
        void Destroy();

        /// <summary>
        /// 设置prop
        /// </summary>
        /// <param name="propsBase"></param>
        void SetProps(APropsBase propsBase);
    }
}