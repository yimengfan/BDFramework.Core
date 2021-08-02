using UnityEngine;

namespace Game.demo6_UFlux._07.Windows_DI
{
    public class Test1Service : ITestService
    {
        public void Log()
        {
            Debug.Log("这是Test1 服务");
        }
    }
}