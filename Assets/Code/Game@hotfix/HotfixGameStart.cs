using BDFramework.GameStart;
using BDFramework.Sql;
using UnityEngine;

namespace Code.Game
{
    [GameStartAtrribute(1)]
    public class HotfixGameStart : IGameStart
    {

      static  public  bool IsAwake { get; private set; }
        public void Start()
        {
          // Application.targetFrameRate = 24;
           BDebug.Log("启动器1 启动成功!" ,"red");
           IsAwake = true;
        }
        
        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}