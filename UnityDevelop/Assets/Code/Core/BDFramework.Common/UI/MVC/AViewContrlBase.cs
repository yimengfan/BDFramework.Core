namespace BDFramework.UI
{
    public class AViewContrlBase : IViewControl
    {
        public AViewContrlBase(DataDrive_Service  data)
        {
            this.DataBinder = data;
        }

        public DataDrive_Service DataBinder { get; private set; }
        
        public void BindData()
        {
            //throw new System.NotImplementedException();
        }
    }
}