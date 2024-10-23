using System;
using System.IO;
using System.Reflection;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixScript;
// using DG.Tweening.Plugins.Core.PathCore;
// using Game.ILRuntime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner;
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
        [MenuItem("BDFrameWork工具箱/TestPipeline/打开TestRunner", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void OpenTestRunner()
        {
            TestRunnerWindow.ShowWindow();
        }

        /// <summary>
        /// 测试所有
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行UnitTest-DLL", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
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
        /// <summary>
        /// ilrutnime的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行UnitTest-ILRuntime", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void UnitTestILRuntime()
        {
            //执行热更单元测试
            var dllPath = IPath.Combine(GameBaseConfigProcessor.GetLoadPath(AssetLoadPathType.DevOpsPublish), BApplication.GetRuntimePlatformPath(), ScriptLoder.DLL_PATH);
            // ILRuntimeHelper.LoadHotfix(dllPath,GameLogicCLRBinding.Bind);
            try
            {
                // ILRuntimeHelper.AppDomain.Invoke("BDFramework.UnitTest.TestRunner", "RunHotfixUnitTest", null, new object[] { });
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            HotfixAssembliesHelper.Dispose();
            //清理当前appdomian 数据
            var path = AssetDatabase.GUIDToAssetPath("2b7a02d2757164147959db0911d95ce6");
            AssetDatabase.ImportAsset(path);
        }

        /// <summary>
        /// ilrutnime的逻辑测试
        /// </summary>
        [MenuItem("BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime(Rebuild DLL)", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepelineEditor)]
        public static void UnitTestILRuntimeWithRebuildDll()
        {
            HotfixScriptTools.BuildDll(BApplication.DevOpsPublishAssetsPath, BApplication.RuntimePlatform, Unity3dRoslynBuildTools.BuildMode.Debug);
            UnitTestILRuntime();
        }
    }
}
