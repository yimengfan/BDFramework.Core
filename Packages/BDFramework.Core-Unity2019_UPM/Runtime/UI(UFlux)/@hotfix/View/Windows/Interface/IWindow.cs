using BDFramework.DataListener;
using BDFramework.UFlux.View.Props;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 窗口接口
    /// </summary>
    public interface IWindow
    {
        T1   GetProps<T1>() where T1 : PropsBase, new();
        void SendMessage(UIMessageData uiMsg);

        /// <summary>
        /// 设置父窗口
        /// </summary>
        /// <param name="window"></param>
        void SetParent(IWindow window);

        IWindow       Parent { get; }
        ADataListener State  { get; }
    }
}