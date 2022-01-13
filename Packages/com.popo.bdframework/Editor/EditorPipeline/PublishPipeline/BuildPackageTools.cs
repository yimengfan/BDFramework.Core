using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using BDFramework.Editor.DevOps;
using BDFramework.Editor.Unity3dEx;
using BDFramework.StringEx;
using DotNetExtension;
using LitJson;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.PublishPipeline
{
    /// <summary>
    /// 构建包体，
    /// 这里是第一次构建母包
    /// </summary>
    static public class BuildPackageTools
    {
        public enum BuildMode
        {
            UseCurrentConfigDebug = -2,
            UseCurrentConfigRelease = -1,
            Debug = 0,
            Release,
        }

        //打包场景
        static string SCENEPATH = "Assets/Scenes/BDFrame.unity";

        static string[] SceneConfigs =
        {
            "Assets/Scenes/Config/Debug.json", //0
            "Assets/Scenes/Config/Release.json" //1
        };


        /// <summary>
        /// build包体工具
        /// </summary>
        static BuildPackageTools()
        {
            //初始化框架编辑器下
            BDFrameworkEditorBehaviour.InitBDFrameworkEditor();
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/Android/Build(当前配置Debug)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildAPKUseCurrentAssets()
        {
            BuildAPK(BuildMode.UseCurrentConfigDebug, false);
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/Android/Build(当前配置Release)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildAPKUseCurrentAssetsRelease()
        {
            BuildAPK(BuildMode.UseCurrentConfigRelease, false);
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/Android/Build(加载Debug.json)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildAPK_Debug()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildAPK(BuildMode.Debug, true);
            }
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/Android/Build(加载Release.json)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildAPK()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildAPK(BuildMode.Release, true);
            }
        }


        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/iOS/Build(当前配置Debug)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildIpaUseCurrentAssets()
        {
            BuildIpa(BuildMode.UseCurrentConfigDebug, false);
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/iOS/Build(当前配置Release)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildIpaUseCurrentAssetsRelease()
        {
            BuildIpa(BuildMode.UseCurrentConfigRelease, false);
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/iOS/Build(加载Debug.json)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildIpa_Debug()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildIpa(BuildMode.Debug, true);
            }
        }

        [MenuItem("BDFrameWork工具箱/PublishPipeline/2.发布母包/iOS/Build(加载Release.json)", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_PublishPackage)]
        public static void EditorBuildIpa()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildIpa(BuildMode.Release, true);
            }
        }

        /// <summary>
        /// 加载场景配置
        /// </summary>
        /// <param name="mode"></param>
        static public void LoadConfig(BuildMode mode)
        {
            var scene = EditorSceneManager.OpenScene(SCENEPATH);
            TextAsset textContent = null;
            if ((int) mode >= 0)
            {
                string path = SceneConfigs[(int) mode];
                textContent = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var config = GameObject.FindObjectOfType<BDLauncher>();
                config.ConfigText = textContent;
                Debug.LogFormat("【BuildPackage】 加载配置:{0} \n {1}", path, config.ConfigText);
            }

            EditorSceneManager.SaveScene(scene);
        }


        #region Android

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// </summary>
        static public void BuildAPK(BuildMode buildMode, bool isGenAssets)
        {
            var outdir = BDApplication.DevOpsPublishPackagePath;
            
            BDFrameworkPublishPipelineHelper.OnBeginBuildPackage(BuildTarget.Android, outdir);
            //0.加载场景和配置
            LoadConfig(buildMode);


            //1.生成资源
            if (isGenAssets)
            {
                EditorWindow_PublishAssets.GenAllAssets(BDApplication.DevOpsPublishAssetsPath, RuntimePlatform.Android);
            }

            //2.拷贝资源并打包
            AssetDatabase.StartAssetEditing(); //停止触发资源导入
            {
                //拷贝资源
                DevOpsTools.CopyPublishAssetsTo(Application.streamingAssetsPath, RuntimePlatform.Android);
                try
                {
                   var outputpath =  BuildAPK(buildMode,outdir);
                   BDFrameworkPublishPipelineHelper.OnEndBuildPackage(BuildTarget.Android, outputpath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                DevOpsTools.DeleteCopyAssets(Application.streamingAssetsPath, RuntimePlatform.Android);
            }
            AssetDatabase.StopAssetEditing(); //恢复触发资源导入
        }

        /// <summary>
        /// 打包APK
        /// </summary>
        static public string BuildAPK(BuildMode mode ,string outdir)
        {
            //删除il2cpp缓存
            DeleteIL2cppCache();

            if (!BDEditorApplication.BDFrameWorkFrameEditorSetting.IsSetConfig())
            {
                Debug.LogError("请注意设置apk keystore账号密码");
                return "";
            }

            var androidConfig = BDEditorApplication.BDFrameWorkFrameEditorSetting.Android;
            //秘钥相关
            PlayerSettings.Android.keystoreName = IPath.Combine(BDApplication.ProjectRoot, androidConfig.keystoreName);
            PlayerSettings.keystorePass = androidConfig.keystorePass;
            PlayerSettings.Android.keyaliasName = androidConfig.keyaliasName;
            PlayerSettings.keyaliasPass = androidConfig.keyaliasPass;
            Debug.Log("【keystore】" + PlayerSettings.Android.keystoreName);
            //具体安卓的配置
            PlayerSettings.gcIncremental = true;
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            //PlayerSettings.stripEngineCode = true;
            // if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) == ManagedStrippingLevel.High)
            // {
            //PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            // }


            //开启符号表
            EditorUserBuildSettings.androidCreateSymbolsZip = true;
            //不同模式的设置
            // switch (mode)
            // {
            //     case BuildMode.Debug:
            //     {
            //         
            //     }
            //         break;
            //     case BuildMode.Release:
            //     {
            //         
            //     }
            //         break;
            // }
            //
         
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}_{2}.apk", Application.identifier, mode.ToString(), DateTimeEx.GetTotalSeconds()));
            //文件夹处理
            if (!Directory.Exists(outdir))
            {
                Directory.CreateDirectory(outdir);
            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            //开始项目一键打包
            string[] scenes = {SCENEPATH};
            BuildOptions opa = BuildOptions.None;
            switch (mode)
            {
                case BuildMode.UseCurrentConfigDebug:
                case BuildMode.Debug:
                {
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
                case BuildMode.UseCurrentConfigRelease:
                case BuildMode.Release:
                {
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }

            //开始构建
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

            return outputPath;
        }

        #endregion

        #region iOS

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// </summary>
        static public void BuildIpa(BuildMode buildMode, bool isGenAssets)
        {
            var outdir = BDApplication.DevOpsPublishPackagePath;
            BDFrameworkPublishPipelineHelper.OnBeginBuildPackage(BuildTarget.iOS, outdir);
            //0.加载场景和配置
            LoadConfig(buildMode);

            //1.生成资源
            if (isGenAssets)
            {
                EditorWindow_PublishAssets.GenAllAssets(BDApplication.DevOpsPublishAssetsPath, RuntimePlatform.IPhonePlayer);
            }

            //2.拷贝资源打包
            AssetDatabase.StartAssetEditing(); //停止触发资源导入
            {
                //拷贝资源
                DevOpsTools.CopyPublishAssetsTo(Application.streamingAssetsPath, RuntimePlatform.IPhonePlayer);
                try
                {
                    var outputpath = BuildIpa(buildMode, outdir);
                    
                    BDFrameworkPublishPipelineHelper.OnEndBuildPackage(BuildTarget.iOS, outputpath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                DevOpsTools.DeleteCopyAssets(Application.streamingAssetsPath, RuntimePlatform.IPhonePlayer);
            }
            AssetDatabase.StopAssetEditing(); //恢复触发资源导入
        }


        /// <summary>
        /// 编译Xcode（这里是出母包版本）
        /// </summary>
        /// <param name="mode"></param>
        static public string BuildIpa(BuildMode mode, string outdir)
        {
            DeleteIL2cppCache();
            //具体IOS的的配置
            PlayerSettings.gcIncremental = true;
            //PlayerSettings.stripEngineCode = true;
            // if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS) == ManagedStrippingLevel.High)
            // {
            // PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Low);
            //}
            //
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}_{2}", Application.identifier, mode.ToString(), DateTimeEx.GetTotalSeconds()));
            //文件夹处理
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            Directory.CreateDirectory(outputPath);


            //开始项目一键打包
            string[] scenes = {SCENEPATH};
            BuildOptions opa = BuildOptions.None;
            switch (mode)
            {
                case BuildMode.UseCurrentConfigDebug:
                case BuildMode.Debug:
                {
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
                case BuildMode.UseCurrentConfigRelease:
                case BuildMode.Release:
                {
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }

            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.iOS, opa);
            if (File.Exists(outputPath + "/Info.plist"))
            {
                Debug.Log("Build Success :" + outputPath + ",后续请配合Jekins出包!");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError(new Exception("【BDFRamework】Build Fail! Please Check the log! "));
            }

            return outputPath;
        }

        #endregion


        /// <summary>
        /// 删除il2cpp
        /// 部分版本下cahce有bug
        /// </summary>
        static private void DeleteIL2cppCache()
        {
#if UNITY_2019 || UNITY_2020
            var directs = Directory.GetDirectories(BDApplication.Library, "*", SearchOption.TopDirectoryOnly);
            foreach (var dirt in directs)
            {
                if (dirt.Contains("il2cpp"))
                {
                    try
                    {
                        Directory.Delete(dirt, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("文件被占用，可能导致il2cpp沿用老的缓存!");
                    }

                    Debug.Log("【删除il2cpp cache】" + dirt);
                }
            }

            //删除
            var tempdirt = Path.Combine(BDApplication.ProjectRoot, "Temp/StagingArea");
            if (Directory.Exists(tempdirt))
            {
                Directory.Delete(tempdirt, true);
            }
#endif
        }
    }
}