using BDFramework.Logic.GameLife;
using UnityEngine;

namespace Code.Game
{
    [GameStartAtrribute(0)]
    public class TestGameStart : IGameStart
    {
        public void Start()
        {
          
            Debug.Log("第一个游戏启动咯! index 0");
        }

        public void Awake()
        {
            
        }

        public void Update()
        {
            
        }
    }
}