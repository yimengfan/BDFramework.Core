using System.Reflection;
using BDFramework;
using BDFramework.GameStart;
using Game.UI;
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
            
            //初始化M_UIMgr
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var t in types)
            {
                M_UIManager.Inst.CheckType(t);
            }
            M_UIManager.Inst.Init();

            
            //加载并显示1号窗口
            M_UIManager.Inst.LoadWindows(1);
            M_UIManager.Inst.ShowWindow(1);

        }

        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}