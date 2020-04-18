using BDFramework.UnitTest;
using Code.Game;

namespace Tests
{
    [HotfixTestOnFrameInitialized(Des = "框架流程测试")]
    static public class BDFramework_OnInitializedTest
    {


        /// <summary>
        /// 测试启动逻辑
        /// </summary>
        [HotfixTestOnFrameInitialized]
        public static void TestStartLogic()
        {
            Assert.Equals(HotfixGameStart.IsAwake, true,"热更启动器未执行");
        }
    }
}