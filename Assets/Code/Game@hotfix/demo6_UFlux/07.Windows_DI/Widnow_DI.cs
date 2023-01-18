using BDFramework.UFlux;
using BDFramework.UI;
using UnityEngine;

namespace Game.demo6_UFlux._07.Windows_DI
{
    [UI((int) WinEnum.Win_UFlux_Test007_DI, "Windows/UFlux/demo007/Window_DI")]
    public class Widnow_DI : AWindow
    {
        public Widnow_DI(string path) : base(path)
        {
        }

        public Widnow_DI(Transform transform) : base(transform)
        {
        }


        private Test1Service testService1;
        private Test2Service testService2;

        /// <summary>
        /// 这里是DI请求对象的接口
        /// </summary>
        /// <param name="service1"></param>
        /// <param name="service2"></param>
        public void Require(Test1Service service1, Test2Service service2)
        {
            this.testService1 = service1;
            this.testService2 = service2;
        }

        public override void Open(UIMsgData uiMsg = null)
        {
            base.Open(uiMsg);
        }
        
        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            //关闭
            this.Close();
        }

        [ButtonOnclick("btn_RequestNet")]
        private void btn_RequestNet()
        {
            this.testService1.Log();
            this.testService2.Log();
        }
    }
}