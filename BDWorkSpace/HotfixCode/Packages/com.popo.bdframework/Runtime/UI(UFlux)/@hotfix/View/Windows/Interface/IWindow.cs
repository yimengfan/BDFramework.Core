using BDFramework.DataListener;
using BDFramework.UFlux.View.Props;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 窗口接口
    /// </summary>
    public interface IWindow
    {
        /// <summary>
        /// 状态管理
        /// </summary>
        ADataListener State { get; }

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