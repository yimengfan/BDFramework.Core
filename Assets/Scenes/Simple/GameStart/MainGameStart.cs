using BDFramework.GameStart;
using UnityEngine;

namespace Game
{
    public class MainGameStart : IGameStart
    {
        public void Start()
        {
            BDebug.Log("主工程代码启动!",Color.red);
        }

        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}