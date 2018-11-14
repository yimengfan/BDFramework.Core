using BDFramework.GameStart;
using BDFramework.Sql;
using UnityEngine;

namespace Code.Game
{
    [GameStartAtrribute(0)]
    public class HotfixGameStart : IGameStart
    {

        public void Start()
        {
           Application.targetFrameRate = 24;
           Debug.Log("第一个游戏启动咯! index 0");
        }
        
        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}