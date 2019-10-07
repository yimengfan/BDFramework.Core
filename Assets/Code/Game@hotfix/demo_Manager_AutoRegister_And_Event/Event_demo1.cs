namespace Code.Game.demo_Manager_AutoRegister_And_Event
{
    
    [DemoEvent((int)DemoEventEnum.TestEvent2)]
    public class Event_demo1:IDemoEvent
    {
        public void Do()
        {
            BDebug.Log("这是demo1的 log","red");
        }
    }
}