using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BDFramework.Editor.TestRunner
{
    // public enum BDEditorMenuEnum
    // {
    //     //测试用例
    //     //Testrunner
    //     TestRunnerEditor =151,
    // }

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
           // BDFramework.UnitTest.TestRunner.RunAPIUnitTest();
           // TestRunner.TestRunnerEditor.RunILRuntimeTest();
            
        }


        /// <summary>
        /// editor的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/执行逻辑测试-Editor", false, (int) BDEditorMenuEnum.TestRunnerEditor)]
        public static void UnitTestEditor()
        {
            RunMonoOrCLRTest();
        }

        /// <summary>
        /// ilrutnime的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/执行逻辑测试-ILRuntime", false, (int) BDEditorMenuEnum.TestRunnerEditor)]
        public static void UnitTestILRuntime()
        {
            RunILRuntimeTest();
        }
        /// <summary>
        /// ilrutnime的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/执行逻辑测试-ILRuntime(Rebuild DLL)", false, (int)BDEditorMenuEnum.TestRunnerEditor)]
        public static void UnitTestILRuntimeWithRebuildDll()
        {
            EditorWindow_ScriptBuildDll.RoslynBuild( Application.streamingAssetsPath,Application.platform,ScriptBuildTools.BuildMode.Debug);
            RunILRuntimeTest();
        }

        /// <summary>
        /// 执行Clrtest
        /// </summary>
        static public void RunMonoOrCLRTest()
        {
            //启动场景
            EditorSceneManager.OpenScene("Assets/Code/BDFramework.UnitTest/MonoCLR.unity");
            
            //执行
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }

        /// <summary>
        /// 执行Clrtest
        /// </summary>
        static public void RunILRuntimeTest()
        {
            //启动场景
            EditorSceneManager.OpenScene("Assets/Code/BDFramework.UnitTest/ILRuntime.unity");
            //执行
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }
    }
}