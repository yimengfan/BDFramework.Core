namespace Code.Game.demo_Manager_AutoRegister_And_Event
{
    
    /// <summary>
    /// 标签一定要加！！！！！！
    /// </summary>
    [DemoEvent((int)DemoEventEnum.TestEvent2)]
    public class Event_demo2:IDemoEvent
    {
        public void Do()
        {
            BDebug.Log("-------------这是demo2的 log---------","yellow");
        }
    }
}