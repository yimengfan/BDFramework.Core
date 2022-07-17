using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.Editor.DevOps;
using BDFramework.Editor.EditorPipeline.BuildPipeline;
using BDFramework.Editor.Environment;
using BDFramework.Editor.PublishPipeline;
using BDFramework.Editor.Tools;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.BuildPipeline
{
    /// <summary>
    /// 构建包体工具
    /// 这里是第一次构建母包
    /// </summary>
    static public class BuildPackageTools
    {
        public enum BuildMode
        {
            /// <summary>
            /// 使用当前配置，debug构建
            /// </summary>
            UseCurrentConfigDebug = -2,

            /// <summary>
            /// 使用当前配置，release构建
            /// </summary>
            UseCurrentConfigRelease = -1,

            /// <summary>
            /// 标准构建，使用Debug配置,Debug构建
            /// </summary>
            Debug = 0,

            /// <summary>
            /// 标准构建，使用Realease配置,Realease构建
            /// </summary>
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
            BDFrameworkEditorEnvironment.InitEditorEnvironment();
        }

        [MenuItem("BDFrameWork工具箱/【BuildPipeline】", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPipeline)]
        public static void NULL()
        {
            EditorWindow_BuildPipeline.Open();
        }


        /// <summary>
        /// 加载场景上的配置
        /// </summary>
        /// <param name="mode"></param>
        static void LoadConfig(BuildMode mode)
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


        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// </summary>
        static public bool Build(BuildMode buildMode, bool isGenAssets, string outdir, BuildTarget buildTarget)
        {
            var buildRuntimePlarform = BApplication.GetRuntimePlatform(buildTarget);
            
            //增加平台路径
            outdir = IPath.Combine(outdir, BApplication.GetPlatformPath(buildTarget));
            BDFrameworkPipelineHelper.OnBeginBuildPackage(buildTarget, outdir);
            //0.加载场景和配置
            LoadConfig(buildMode);

            //1.生成资源到Devops
            if (isGenAssets)
            {
                BuildAssetsTools.BuildAllAssets(buildRuntimePlarform, BApplication.DevOpsPublishAssetsPath);
            }

            bool buildResult = false;
            string outputpath = "";
            //2.拷贝资源并打包
            AssetDatabase.StartAssetEditing(); //停止触发资源导入
            {
                //拷贝资源
                DevOpsTools.CopyPublishAssetsTo(Application.streamingAssetsPath, buildRuntimePlarform);
                try
                {
                    switch (buildTarget)
                    {
                        case BuildTarget.Android:
                        {
                            (buildResult, outputpath) = BuildAPK(buildMode, outdir);
                        }
                            break;
                        case BuildTarget.iOS:
                        {
                            (buildResult, outputpath) = BuildIpa(buildMode, outdir);
                        }
                            break;
                        case BuildTarget.StandaloneWindows:
                        case BuildTarget.StandaloneWindows64:
                        {
                        }
                            break;
                        case BuildTarget.StandaloneOSX:
                        {
                        }
                            break;
                        default:
                        {
                            throw new Exception("未实现打包平台:" + buildTarget);
                        }
                            break;
                    }

                    BDFrameworkPipelineHelper.OnEndBuildPackage(buildTarget, outputpath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                DevOpsTools.DeleteCopyAssets(Application.streamingAssetsPath, buildRuntimePlarform);
            }
            AssetDatabase.StopAssetEditing(); //恢复触发资源导入

            return buildResult;
        }


        #region Android

        /// <summary>
        /// 打包APK
        /// </summary>
        static private (bool, string) BuildAPK(BuildMode mode, string outdir)
        {
            bool ret = false;
            //切换到Android
            BDEditorApplication.SwitchToAndroid();
            //删除il2cpp缓存
            //DeleteIL2cppCache();

            if (!BDEditorApplication.BDFrameworkEditorSetting.IsSetConfig())
            {
                //For ci
                throw new Exception("请注意设置apk keystore账号密码");
            }

            //模式
            AndroidSetting androidConfig = null;
            switch (mode)
            {
                case BuildMode.Debug:
                case BuildMode.UseCurrentConfigDebug:
                {
                    androidConfig = BDEditorApplication.BDFrameworkEditorSetting.AndroidDebug;
                }
                    break;
                case BuildMode.Release:
                case BuildMode.UseCurrentConfigRelease:
                {
                    androidConfig = BDEditorApplication.BDFrameworkEditorSetting.Android;
                }
                    break;
            }


            //秘钥相关
            var keystorePath = IPath.Combine(BApplication.ProjectRoot, androidConfig.keystoreName);
            if (!File.Exists(keystorePath))
            {
                //For ci
                throw new Exception("【keystore】不存在:" + keystorePath);
            }

            PlayerSettings.Android.keystoreName = keystorePath;
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
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}.apk", Application.identifier, mode.ToString()));
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

            //构建包体
            Debug.Log("------------->Begin build<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, opa);
            Debug.Log("------------->End build<------------");

            //构建出判断
            if (File.Exists(outputPath))
            {
                Debug.Log("Build Success :" + outputPath);
                ret = true;
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                //For ci
                throw new Exception("【BDFramework】Package not exsit！ -" + outputPath);
            }

            return (ret, outputPath);
        }

        #endregion

        #region iOS

        /// <summary>
        /// 编译Xcode（这里是出母包版本）
        /// </summary>
        /// <param name="mode"></param>
        static private (bool, string) BuildIpa(BuildMode mode, string outdir)
        {
            bool ret = false;
            BDEditorApplication.SwitchToiOS();
            //DeleteIL2cppCache();
            //具体IOS的的配置
            PlayerSettings.gcIncremental = true;
            //PlayerSettings.stripEngineCode = true;
            // if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS) == ManagedStrippingLevel.High)
            // {
            // PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Low);
            //}
            //
            //文件夹处理
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}", Application.identifier, mode.ToString()));
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            Directory.CreateDirectory(outputPath);


            //开始项目一键打包
            string[] scenes = {SCENEPATH};
            BuildOptions opa = BuildOptions.None;
            BuildMode realmode = BuildMode.Release;
            switch (mode)
            {
                case BuildMode.UseCurrentConfigDebug:
                case BuildMode.Debug:
                {
                    realmode = BuildMode.Debug;
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
                case BuildMode.UseCurrentConfigRelease:
                case BuildMode.Release:
                {
                    realmode = BuildMode.Release;
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }

            //构建包体
            Debug.Log("------------->Begin build<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.iOS, opa);
            Debug.Log("------------->End build<------------");


            //检测xcode
            if (File.Exists(outputPath + "/Info.plist"))
            {
                var shellPath = "";
                //这里需要判断Editor类型,所以使用u3d的 Application.Platform
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    shellPath = BApplication.DevOpsCIPath + string.Format("/BuildIpa_{0}.cmd", realmode);
                }
                else if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    shellPath = BApplication.DevOpsCIPath + string.Format("/BuildIpa_{0}.shell", realmode);
                }

                if (File.Exists(shellPath))
                {
                    //执行BuildIpa的shell
                    Debug.Log("即将执行:" + shellPath);
                    CMDTools.RunCmdFile(shellPath);
                    //删除xcode项目
                    Directory.Delete(outputPath, true);

                    var ipaPath = outputPath + ".ipa";
                    if (File.Exists(ipaPath))
                    {
                        ret = true;
                    }
                    else
                    {
                        Debug.LogError("【BDFramework】 not found:" + ipaPath);
                    }
                }
                else
                {
                    //For ci
                    throw new Exception("没找到编译xcode脚本! 后续请配合Jekins/Teamcity出包!");
                }

                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                //For ci
                throw new Exception("【BDFramework】Package not exsit！ -" + outputPath);
            }

            return (ret, outputPath);
        }

        #endregion

        #region Windows

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// </summary>
        static public bool BuildExe(BuildMode buildMode, bool isGenAssets, string outdir)
        {
            bool ret = false;
            //增加平台路径
            outdir = IPath.Combine(outdir, BApplication.GetPlatformPath(BuildTarget.StandaloneWindows64));
            BDFrameworkPipelineHelper.OnBeginBuildPackage(BuildTarget.StandaloneWindows64, outdir);
            //0.加载场景和配置
            LoadConfig(buildMode);

            //1.生成资源
            if (isGenAssets)
            {
                BuildAssetsTools.BuildAllAssets(RuntimePlatform.IPhonePlayer, BApplication.DevOpsPublishAssetsPath);
            }

            //2.拷贝资源打包
            AssetDatabase.StartAssetEditing(); //停止触发资源导入
            {
                //拷贝资源
                DevOpsTools.CopyPublishAssetsTo(Application.streamingAssetsPath, RuntimePlatform.IPhonePlayer);
                try
                {
                    var (_ret, outputpath) = BuildIpa(buildMode, outdir);
                    BDFrameworkPipelineHelper.OnEndBuildPackage(BuildTarget.iOS, outputpath);
                    ret = _ret;
                }
                catch (Exception e)
                {
                    //For ci
                    throw e;
                }

                DevOpsTools.DeleteCopyAssets(Application.streamingAssetsPath, RuntimePlatform.IPhonePlayer);
            }
            AssetDatabase.StopAssetEditing(); //恢复触发资源导入

            return ret;
        }


        /// <summary>
        /// 编译Xcode（这里是出母包版本）
        /// </summary>
        /// <param name="mode"></param>
        static private (bool, string) BuildExe(BuildMode mode, string outdir)
        {
            bool ret = false;
            BDEditorApplication.SwitchToWindows();
            //DeleteIL2cppCache();
            //具体IOS的的配置
            PlayerSettings.gcIncremental = true;
            //PlayerSettings.stripEngineCode = true;
            // if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS) == ManagedStrippingLevel.High)
            // {
            // PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Low);
            //}
            //
            //文件夹处理
            var outputPath = IPath.Combine(outdir, string.Format("{0}_{1}", Application.identifier, mode.ToString()));
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            Directory.CreateDirectory(outputPath);


            //开始项目一键打包
            string[] scenes = {SCENEPATH};
            BuildOptions opa = BuildOptions.None;
            BuildMode realmode = BuildMode.Release;
            switch (mode)
            {
                case BuildMode.UseCurrentConfigDebug:
                case BuildMode.Debug:
                {
                    realmode = BuildMode.Debug;
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
                case BuildMode.UseCurrentConfigRelease:
                case BuildMode.Release:
                {
                    realmode = BuildMode.Release;
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }

            //构建包体
            Debug.Log("------------->Begin build<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.iOS, opa);
            Debug.Log("------------->End build<------------");


            //检测xcode
            if (File.Exists(outputPath + "/Info.plist"))
            {
            }
            else
            {
                //For ci
                throw new Exception("【BDFramework】Package not exsit！ -" + outputPath);
            }

            return (ret, outputPath);
        }

        #endregion

        #region Mac OSX

        #endregion

        /// <summary>
        /// 删除il2cpp
        /// 部分版本下cahce有bug
        /// </summary>
        static private void DeleteIL2cppCache()
        {
#if UNITY_2019
            var directs = Directory.GetDirectories(BApplication.Library, "*", SearchOption.TopDirectoryOnly);
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
            var tempdirt = Path.Combine(BApplication.ProjectRoot, "Temp/StagingArea");
            if (Directory.Exists(tempdirt))
            {
                Directory.Delete(tempdirt, true);
            }
#endif
        }
    }
}
