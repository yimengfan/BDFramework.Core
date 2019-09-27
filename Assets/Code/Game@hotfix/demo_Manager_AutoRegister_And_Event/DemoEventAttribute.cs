using BDFramework.Mgr;

namespace Code.Game.demo_Manager_AutoRegister_And_Event
{
    public class DemoEventAttribute :  ManagerAtrribute
    {
        
        
        public DemoEventAttribute(DemoEventEnum eventEnum) : base(eventEnum.ToString())
        {
            
        }
    }
}