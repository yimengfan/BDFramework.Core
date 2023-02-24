using BDFramework.GameStart;
using BDFramework.Sql;
using UnityEngine;

namespace Game
{
    public class HotFixGameStart : IHotfixGameStart
    {
        static public bool IsAwake { get; private set; }

        public void Start()
        {
            // Application.targetFrameRate = 24;
            BDebug.Log("热更启动成功!", "red");
            IsAwake = true;
            Client.Init();
        }

        public void Update()
        {
        }

        public void LateUpdate()
        {
        }
    }
}