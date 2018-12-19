namespace Game.UI
{
    public class M_AIViewContrlBase : AIEnumeratorTaskMgr, M_IViewControl
    {
        public M_AIViewContrlBase(DataListenerService  data)
        {
            this.Model = data;
        }

        public DataListenerService Model { get; private set; }
        
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