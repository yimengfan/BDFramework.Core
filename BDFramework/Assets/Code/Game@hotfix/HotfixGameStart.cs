using BDFramework.GameStart;
using BDFramework.Sql;
using UnityEngine;

namespace Code.Game
{
    [GameStartAtrribute(1)]
    public class HotfixGameStart : IGameStart
    {

        public void Start()
        {
           Application.targetFrameRate = 24;
           Debug.Log("热更代码准备完毕!");
        }
        
        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}