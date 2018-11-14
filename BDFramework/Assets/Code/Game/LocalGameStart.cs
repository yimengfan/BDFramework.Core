using BDFramework;
using BDFramework.GameStart;
using UnityEngine;

namespace Game
{
    [GameStartAtrribute(0)]
    public class LocalGameStart : IGameStart
    {
        public void Start()
        {
            BDebug.Log("本地代码启动!");
            BDebug.Log("准备启动热更逻辑!");
            //
            GameObject.Find("BDFrame").GetComponent<BDLauncher>().LaunchHotFix();
        }

        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}