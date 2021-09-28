using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using BDFramework.Editor.Asset;
using BDFramework.Editor.BuildPackage;
using BDFramework.Core.Tools;
using BDFramework.Editor;
using UnityEditor.SceneManagement;

namespace BDFramework.Editor
{
    /// <summary>
    /// 构建包体，
    /// 这里是第一次构建母包
    /// </summary>
    static public class EditorBuildPackage
    {
        public enum BuildMode
        {
            Debug = 0,
            Release,
        }

        static string   SCENEPATH    = "Assets/Scenes/BDFrame.unity";
        static string[] SceneConfigs = { "Assets/Scenes/Config/Debug.json", "Assets/Scenes/Config/Release.json", };


        static EditorBuildPackage()
        {
            //初始化框架编辑器下
            BDFrameEditorLife.InitBDFrameworkEditor();
        }


        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(使用当前配置、资源)")]
        public static void EditorBuildAPKUseCurrentAssets()
        {
            BuildAPK();
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(Config-Debug.json)")]
        public static void EditorBuildAPK_Debug()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildDebugAPK();
            }
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(Config-Release.json)")]
        public static void EditorBuildAPK()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildReleaseAPK();
            }
        }


        [MenuItem("BDFrameWork工具箱/打包/BuildIOS(使用当前配置、资源)")]
        public static void EditorBuildIpaUseCurrentAssets()
        {
            BuildEmptyIpa();
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildIOS(Config-Debug.json)")]
        public static void EditorBuildIpa_Debug()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildDebugIpa();
            }
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildIOS(Config-Release.json)")]
        public static void EditorBuildIpa()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildReleaseIpa();
            }
        }






        /// <summary>
        /// 加载场景配置
        /// </summary>
        /// <param name="mode"></param>
        static public void LoadConfig(BuildMode? mode = null)
        {
            var       scene       = EditorSceneManager.OpenScene(SCENEPATH);
            TextAsset textContent = null;
            if (mode != null)
            {
                string path = SceneConfigs[(int)mode];
                textContent = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var config = GameObject.FindObjectOfType<BDLauncher>();
                config.ConfigText = textContent;
            }

            EditorSceneManager.SaveScene(scene);
        }




        #region Android

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// </summary>
        static public void BuildAPK()
        {
            LoadConfig();
            BuildAPK(BuildMode.Debug);
        }

        /// <summary>
        /// 构建Debug包体
        /// </summary>
        static public void BuildDebugAPK()
        {
            LoadConfig(BuildMode.Debug);
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath, RuntimePlatform.Android, BuildTarget.Android);
            BuildAPK(BuildMode.Debug);
        }

        /// <summary>
        /// 构建Release包体
        /// </summary>
        static public void BuildReleaseAPK()
        {
            LoadConfig(BuildMode.Release);
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath, RuntimePlatform.Android, BuildTarget.Android);
            BuildAPK(BuildMode.Release);
        }


        /// <summary>
        /// 打包APK
        /// </summary>
        static public void BuildAPK(BuildMode mode)
        {
            if (!BDEditorApplication.BdFrameEditorSetting.IsSetConfig())
            {
                Debug.LogError("请注意设置apk keystore账号密码");
                return;
            }

            var absroot = Application.dataPath.Replace("Assets", "");
            PlayerSettings.Android.keystoreName = absroot + BDEditorApplication.BdFrameEditorSetting.Android.keystoreName;
            PlayerSettings.keystorePass         = BDEditorApplication.BdFrameEditorSetting.Android.keystorePass;
            PlayerSettings.Android.keyaliasName = BDEditorApplication.BdFrameEditorSetting.Android.keyaliasName;
            PlayerSettings.keyaliasPass         = BDEditorApplication.BdFrameEditorSetting.Android.keyaliasPass;
            //具体安卓的配置
            PlayerSettings.gcIncremental                    = true;
            PlayerSettings.stripEngineCode                  = true;
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            //
            var outdir     = BDApplication.ProjectRoot + "/Build";
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}.apk", Application.productName, mode.ToString()));
            //文件夹处理
            if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);
            if (File.Exists(outputPath)) File.Delete(outputPath);
            //清空StreamingAsset
            var ios = IPath.Combine(Application.streamingAssetsPath, "iOS");
            if (Directory.Exists(ios))
            {
                Directory.Delete(ios, true);
            }

            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win, true);
            }

            //开始项目一键打包
            string[]     scenes = { SCENEPATH };
            BuildOptions opa    = BuildOptions.None;
            if (mode == BuildMode.Debug)
            {
                opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
            }
            else if (mode == BuildMode.Release)
            {
                opa = BuildOptions.CompressWithLz4HC;
            }

            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, opa);
            if (File.Exists(outputPath))
            {
                Debug.Log("Build Success :" + outputPath);
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError(new Exception("Build Fail! Please Check the log! "));
            }
        }

        #endregion

        #region iOS

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// </summary>
        static public void BuildEmptyIpa()
        {
            LoadConfig();
            BuildIpa(BuildMode.Debug);
        }

        /// <summary>
        /// 编译Debug版本Ipa
        /// </summary>
        static public void BuildDebugIpa()
        {
            LoadConfig(BuildMode.Debug);
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath, RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
            BuildIpa(BuildMode.Debug);
        }

        /// <summary>
        /// 编译release版本Ipa
        /// </summary>
        static public void BuildReleaseIpa()
        {
            LoadConfig(BuildMode.Release);
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath, RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
            BuildIpa(BuildMode.Release);
        }

        /// <summary>
        /// 编译Xcode（这里是出母包版本）
        /// </summary>
        /// <param name="mode"></param>
        static public void BuildIpa(BuildMode mode)
        {
            //具体IOS的的配置
            PlayerSettings.gcIncremental   = true;
            PlayerSettings.stripEngineCode = true;
            //
            var outdir     = BDApplication.ProjectRoot + "/Build";
            
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}", Application.productName, mode.ToString()));
            //文件夹处理
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            Directory.CreateDirectory(outputPath);

            //清空StreamingAsset
            var android = IPath.Combine(Application.streamingAssetsPath, "Android");
            if (Directory.Exists(android))
            {
                Directory.Delete(android, true);
            }

            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win, true);
            }

            //开始项目一键打包
            string[]     scenes = { SCENEPATH };
            BuildOptions opa    = BuildOptions.None;
            if (mode == BuildMode.Debug)
            {
                opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
            }
            else if (mode == BuildMode.Release)
            {
                opa = BuildOptions.CompressWithLz4HC;
            }

            BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.iOS, opa);
            if (File.Exists(outputPath + "/Info.plist"))
            {
                Debug.Log("Build Success :" + outputPath + ",后续请配合Jekins出包!");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError(new Exception("【BDFRamework】Build Fail! Please Check the log! "));
            }
        }

        #endregion
    }
}