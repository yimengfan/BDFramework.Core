using BDFramework.UnitTest;
using Game;
namespace Tests
{
    [UnitTestAttribute(des: "框架流程测试")]
    static public class LogicTest_BDFrameworkStartCheck
    {


        /// <summary>
        /// 测试启动逻辑
        /// </summary>
        [UnitTest]
        public static void GameStartLogic()
        {
            Assert.Equals(HotfixGameStart.IsAwake, true,"热更启动器未执行");
        }
    }
}