using UnityEditor;

namespace BDFramework.Editor.TestRunner
{
    /// <summary>
    /// 测试
    /// </summary>
    static public class TestRunnerEditor
    {

        /// <summary>
        /// 测试所有
        /// </summary>
        [MenuItem("BDFrameWork工具箱/执行框架UnitTest-API", false, (int) BDEditorMenuEnum.TestRunnerEditor)]
        public static void UnitTest()
        {
            BDFramework.UnitTest.TestRunner.RunUnitTest();
        }
        

    }
}