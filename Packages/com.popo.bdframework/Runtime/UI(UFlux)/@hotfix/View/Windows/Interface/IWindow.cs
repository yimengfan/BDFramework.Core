using System.Collections.Generic;
using BDFramework.DataListener;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 窗口接口
    /// </summary>
    public interface IWindow
    {
        /// <summary>
        /// 根节点窗口
        /// </summary>
         IWindow Root { get; set; }
        /// <summary>
        /// 父节点
        /// </summary>
         IWindow Parent { get; set; }
        
        /// <summary>
        /// 组件列表
        /// </summary>
        List<IComponent> ComponentList { get; }
        /// <summary>
        /// 状态管理,事件监听
        /// </summary>
        AStatusListener State { get; }
        /// <summary>
        /// 是否处于激活状态
        /// </summary>
        bool IsFocus { get; }

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <param name="coms"></param>
        void AddComponent(params IComponent[] coms);
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="uiMsg"></param>
        void SendMessage(UIMsgData uiMsg);

        /// <summary>
        /// 开启
        /// </summary>
        /// <param name="uiMsg"></param>
        /// <param name="isFocus"></param>
        void Open(UIMsgData uiMsg = null);

        /// <summary>
        /// 关闭
        /// </summary>
        void Close();
        
        /// <summary>
        /// 当窗口重新获得焦点时会调用
        /// 如 2覆盖1上面，2关闭，1触发focus
        /// </summary>
        void OnFocus();

        /// <summary>
        /// 丢失焦点
        /// </summary>
        void LoseFocus();

        /// <summary>
        /// 注册子窗口
        /// </summary>
        /// <param name="subwin"></param>
        void RegisterSubWindow(IWindow subwin);

        /// <summary>
        /// 获取子窗口
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        T1 GetSubWindow<T1>() where T1 : class;
    }
}