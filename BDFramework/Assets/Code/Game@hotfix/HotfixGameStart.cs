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
           BDebug.Log("hotfix代码 启动器连接成功!" ,"red");
        }
        
        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}