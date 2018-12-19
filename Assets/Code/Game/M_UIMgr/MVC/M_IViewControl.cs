namespace Game.UI
{
    public interface M_IViewControl
    {
        DataListenerService Model { get; }

        void BindModel();
        void Enter();
        void Exit();
    }
}