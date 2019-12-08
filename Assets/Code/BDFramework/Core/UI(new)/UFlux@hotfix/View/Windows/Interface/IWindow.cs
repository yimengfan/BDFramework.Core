
using BDFramework.UFlux.View.Props;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 窗口基类
    /// </summary>
    public interface IWindow
    {
        T1 GetProps<T1>() where T1 : PropsBase, new();
        void SendMessage(UIMessageData uiMsg);
    }
}