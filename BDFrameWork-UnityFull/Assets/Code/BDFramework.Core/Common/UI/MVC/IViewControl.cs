namespace BDFramework.UI
{
    public interface IViewControl
    {
        DataDrive_Service Model { get; }

        void BindModel();
        void Enter();
        void Exit();
    }
}