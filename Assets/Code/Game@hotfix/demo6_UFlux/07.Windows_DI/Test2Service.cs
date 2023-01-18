using UnityEngine;

namespace Game.demo6_UFlux._07.Windows_DI
{
    public class Test2Service : ITestService
    {
        public void Log()
        {
            Debug.Log("这是Test2 服务");
        }
    }
}