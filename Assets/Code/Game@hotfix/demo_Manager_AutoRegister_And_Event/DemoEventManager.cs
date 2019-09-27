using BDFramework.Mgr;

namespace Code.Game.demo_Manager_AutoRegister_And_Event
{
    /// <summary>
    /// DemoEventManger的管理类
    /// </summary>
    public class DemoEventManager : ManagerBase<DemoEventManager, DemoEventAttribute>
    {
        /// <summary>
        /// 管理器的
        /// </summary>
        /// <param name="enum">event名</param>
        /// <param name="o">用于后期传值,这边只用object代替</param>
        public void Do(DemoEventEnum @enum, object o =null)
        {
            //这里是每次创建的一个，
            //实际使用中,使用者可以对创建的对象群体进行管理，如池化 等
            var _event = CreateInstance<IDemoEvent>(@enum.ToString());

            if (_event != null)
            {
                _event.Do();
            }
            
        }
    }
}