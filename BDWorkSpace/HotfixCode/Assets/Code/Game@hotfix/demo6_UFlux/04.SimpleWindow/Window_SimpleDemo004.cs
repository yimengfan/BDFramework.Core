using System;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.UFluxTest004
{
    public enum WinMsg
    {
        testMsg = 0,
    }
    
    /// <summary>
    /// 这个是最简单的窗口模型，
    /// 可以基于这个做任何逻辑，
    /// 不强制使用flux
    /// </summary>
    [UI((int) WinEnum.Win_UFlux_Test004, "Windows/UFlux/demo004/Window_SimpleWindow")]
    public class Window_SimpleDemo004 : AWindow
    {
        public Window_SimpleDemo004(string path) : base(path)
        {
        }

        public Window_SimpleDemo004(Transform transform) : base(transform)
        {
        }

        [TransformPath("Content")]
        private Text Content;

        public override void Init()
        {
            base.Init();

            //注册子窗口
            RegisterSubWindow(new SubWindow_Demo004(this.Transform.Find("SubWindow")));
        }

        [ButtonOnclick("btn_OpenSubWin")]
        private void btn_OpenSubWin()
        {
            GetSubWindow<SubWindow_Demo004>().Open();
        }

        [ButtonOnclick("btn_CloseSubWin")]
        private void btn_CloseSubWin()
        {
            GetSubWindow<SubWindow_Demo004>().Close();
        }

        [ButtonOnclick("btn_Close")]
        private void btn_close()
        {
            this.Close();
        }
        
        /// <summary>
        /// 测试消息
        /// </summary>
        public class UIMsg_Test: UIMsgData
        {
            public string MsgContent;
        }
        
        [ButtonOnclick("btn_SendMessage")]
        private void btn_SndMessage()
        {
            var msg =new UIMsg_Test()
            {
                MsgContent = "我是一个测试消息!\n time:"+ DateTime.Now.ToLongTimeString()
            };
            UIManager.Inst.SendMessage(WinEnum.Win_UFlux_Test004, msg);
        }
        

        
        [UIMessageListener]
        private void TestMessage(Window_SimpleDemo004.UIMsg_Test msg)
        {
            Content.text = "父窗口收到消息:" + msg.MsgContent;
        }
    }
}