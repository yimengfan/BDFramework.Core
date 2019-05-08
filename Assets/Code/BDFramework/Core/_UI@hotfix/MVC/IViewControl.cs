namespace BDFramework.UI
{
    using  BDFramework.DataListener;
    public interface IViewControl
    {
        DataListenerService Model { get; }

        void BindModel();
        void Enter();
        void Exit();
    }
}