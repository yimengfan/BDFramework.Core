using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UI;
using BDFramework.UnitTest;

namespace Code.BDFramework.UnitTest
{
    [UnitTest(des: "UIManager测试")]
    static public class APITest_UIManager
    {
        [UnitTest(des: "【同步】单窗口测试")]
        static public void SyncSingleWindowTest()
        {
            UIManager.Inst.LoadWindow(WinEnum.Win_Demo5_Atlas);
            var win = UIManager.Inst.GetWindow(WinEnum.Win_Demo5_Atlas)as IComponent;
            Assert.IsPass(win.IsLoad, "窗口加载失败");
            
            UIManager.Inst.CloseWindow(WinEnum.Win_Demo5_Atlas);
            UIManager.Inst.UnLoadWindow(WinEnum.Win_Demo5_Atlas);
            Assert.IsPass(true);
        }

        [UnitTest(des: "【异步】单窗口测试")]
        static public void AsyncSingleWindowTest()
        {
            UIManager.Inst.AsyncLoadWindow(WinEnum.Win_Demo_Datalistener, () =>
            {
                var win = UIManager.Inst.GetWindow(WinEnum.Win_Demo_Datalistener)as IComponent;
                Assert.IsPass(win.IsLoad, "窗口加载失败");
                UIManager.Inst.CloseWindow(WinEnum.Win_Demo_Datalistener);
                UIManager.Inst.UnLoadWindow(WinEnum.Win_Demo_Datalistener);
                Assert.IsPass(true);
            });
        }


        [UnitTest(des: "【异步】多窗口测试")]
        static public void AsyncMultipleWindowsTest()
        {
            var list = new List<int>() {(int) WinEnum.Win_Demo5_Atlas, (int) WinEnum.Win_Demo_Datalistener};
            UIManager.Inst.AsyncLoadWindows(list, (i, j) =>
            {
                if (i == j)
                {
                    var win = UIManager.Inst.GetWindow(WinEnum.Win_Demo5_Atlas) as IComponent;
                    Assert.IsPass(win.IsLoad, "窗口加载失败");
                    UIManager.Inst.ShowWindow(WinEnum.Win_Demo5_Atlas);
                    UIManager.Inst.CloseWindow(WinEnum.Win_Demo5_Atlas);


                    win = UIManager.Inst.GetWindow(WinEnum.Win_Demo_Datalistener) as IComponent;
                    Assert.IsPass(win.IsLoad, "窗口加载失败");
                    UIManager.Inst.ShowWindow(WinEnum.Win_Demo_Datalistener);
                    UIManager.Inst.CloseWindow(WinEnum.Win_Demo_Datalistener);

                    Assert.IsPass(true);
                }
                
            });
        }
    }
}