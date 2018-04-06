namespace BDFramework.UI
{
    public interface IViewControl
    {
        DataDrive_Service DataBinder { get; }

        void BindData();
        void Enter();
        void Exit();
    }
}