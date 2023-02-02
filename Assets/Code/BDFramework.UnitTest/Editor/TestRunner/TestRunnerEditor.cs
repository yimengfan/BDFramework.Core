using System;
using System.IO;
using System.Reflection;
using BDFramework.Core.Tools;
using DG.Tweening.Plugins.Core.PathCore;
using Game.ILRuntime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BDFramework.Editor.TestRunner
{
    // public enum BDEditorMenuEnum
    // {
    //     //测试用例
    //     //Testrunner
    //     TestPepelineEditor =151,
    // }

    /// <summary>
    /// 测试
    /// </summary>
    static public class TestRunnerEditor
    {
        /// <summary>
        /// 测试所有
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行框架UnitTest-API", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void UnitTest()
        {
            var assemblys = AppDomain.CurrentDomain.GetAssemblies();
            //反射执行
            foreach (var assembly in assemblys)
            {
                var type = assembly.GetType("BDFramework.UnitTest.TestRunner");
                if (type != null)
                {
                    var mi = type.GetMethod("RunMonoCLRUnitTest", BindingFlags.Public | BindingFlags.Static);
                    if (mi != null)
                    {
                        mi.Invoke(null, new object[] { });
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// editor的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行逻辑测试-Editor", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void UnitTestEditor()
        {
            RunMonoOrCLRTest();
        }

        /// <summary>
        /// ilrutnime的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void UnitTestILRuntime()
        {
            //执行热更单元测试
            var dllPath =IPath.Combine(GameConfig.GetLoadPath(AssetLoadPathType.DevOpsPublish), BApplication.GetRuntimePlatformPath(), ScriptLoder.DLL_PATH);
            ILRuntimeHelper.LoadHotfix(dllPath,GameLogicCLRBinding.Bind);
            try
            {
                ILRuntimeHelper.AppDomain.Invoke("BDFramework.UnitTest.TestRunner", "RunHotfixUnitTest", null, new object[] { });
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
          
            ILRuntimeHelper.Dispose();
        }

        /// <summary>
        /// ilrutnime的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime(Rebuild DLL)", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void UnitTestILRuntimeWithRebuildDll()
        {
            EditorWindow_ScriptBuildDll.RoslynBuild(BApplication.DevOpsPublishAssetsPath, BApplication.RuntimePlatform, ScriptBuildTools.BuildMode.Debug);
            UnitTestILRuntime();
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
