
using BDFramework.DataListener;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 窗口接口
    /// </summary>
    public interface IWindow
    {
        /// <summary>
        /// 状态管理,事件监听
        /// </summary>
        AStatusListener State { get; }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="uiMsg"></param>
        void SendMessage(UIMsgData uiMsg);

        /// <summary>
        /// 设置父窗口
        /// </summary>
        /// <param name="window"></param>
        void SetParent(IWindow window);

        /// <summary>
        /// 父窗口
        /// </summary>
        IWindow Parent { get; }
        
        /// <summary>
        /// 打开
        /// </summary>
        void Open(UIMsgData uiMsg =null);

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