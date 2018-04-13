namespace BDFramework.UI
{
    public class AViewContrlBase : AIEnumeratorTaskMgr, IViewControl
    {
        public AViewContrlBase(DataDrive_Service  data)
        {
            this.Model = data;
        }

        public DataDrive_Service Model { get; private set; }
        
        virtual public void BindModel()
        {
        }

        /// <summary>
        /// 进入模块
        /// </summary>
       virtual  public void Enter()
       {

       }

        /// <summary>
        /// 退出
        /// </summary>
        virtual public void Exit()
        {
            this.StopAllCroutine();
        }
    }
}