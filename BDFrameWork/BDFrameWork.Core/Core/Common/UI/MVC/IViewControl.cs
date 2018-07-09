namespace BDFramework.UI
{
    public interface IViewControl
    {
        DataDriven_Service Model { get; }

        void BindModel();
        void Enter();
        void Exit();
    }
}