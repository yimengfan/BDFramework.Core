using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.Environment;
using BDFramework.Editor.Tools;
using BDFramework.Editor.Tools.RuntimeEditor;
using BDFramework.ResourceMgr;
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
            /// 标准构建，使用Debug配置,Debug构建
            /// </summary>
            Debug = 0,

            /// <summary>
            /// Release 发布
            /// </summary>
            Release,

            /// <summary>
            /// Release for profiler，
            /// Release编译但是开启
            /// </summary>
            Profiler,
        }

        //打包场景
        readonly public static string SCENE_PATH = "Assets/Scenes/BDFrame.unity";
        readonly public static string QA_SCENE_PATH = "Assets/Scenes/BDFrameForQA.unity";

        readonly static public string[] SceneConfigs =
        {
            "Assets/Scenes/Config/Debug.bytes", //0
            "Assets/Scenes/Config/Release.bytes" //1
        };


        /// <summary>
        /// build包体工具
        /// </summary>
        static BuildPackageTools()
        {
            //初始化框架编辑器下
            BDFrameworkEditorEnvironment.InitEditorEnvironment();
        }


        /// <summary>
        /// 加载场景上的配置
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="buildScene"></param>
        static void LoadConfig(string buildScene, string buildConfig)
        {
            var scene = EditorSceneManager.OpenScene(buildScene);
            //打开场景保存配置
            TextAsset textContent = null;
            textContent = AssetDatabase.LoadAssetAtPath<TextAsset>(buildConfig);
            var config = GameObject.FindObjectOfType<BDLauncher>();
            config.ConfigText = textContent;
            Debug.LogFormat("【BuildPackage】 加载配置:{0} \n {1}", buildConfig, config.ConfigText);
            //保存场景
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// 这里默认建议使用单场景结构打包
        /// </summary>
        static public bool Build(BuildMode buildMode, bool isGenAssets, string outdir, BuildTarget buildTarget,
            BuildAssetsTools.BuildPackageOption buildOption = BuildAssetsTools.BuildPackageOption.BuildAll)
        {
            string buildConfig = "";
            switch (buildMode)
            {
                case BuildMode.Debug:
                case BuildMode.Profiler:
                {
                    buildConfig = SceneConfigs[0];
                }
                    break;
                case BuildMode.Release:
                {
                    buildConfig = SceneConfigs[1];
                }
                    break;
            }

            //build
            return Build(buildMode, SCENE_PATH, buildConfig, isGenAssets, outdir, buildTarget, buildOption);
        }

        static public bool IsBuilding { get;private set; } = false;
        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// 这里默认建议使用单场景结构打包.
        /// </summary>
        static public bool Build(BuildMode buildMode, string buildScene, string buildConfig, bool isGenAssets,
            string outdir, BuildTarget buildTarget,
            BuildAssetsTools.BuildPackageOption buildOption = BuildAssetsTools.BuildPackageOption.BuildAll)
        {
            if (IsBuilding)
            {
                return false;
            }
            IsBuilding = true;
            //开始构建流程
            string addPackageNameStr = null;
            if (buildMode != BuildMode.Release)
            {
                addPackageNameStr = "." + buildMode.ToString().ToLower();
            }

            //不同模式的设置
            switch (buildMode)
            {
                case BuildMode.Debug:
                case BuildMode.Profiler:
                {
                    BDebugEditor.EnableDebug();
                }
                    break;
                case BuildMode.Release:
                {
                    BDebugEditor.DisableDebug();
                }
                    break;
            }

            AssetDatabase.Refresh();

            //不通模式的设置
            //项目名
            string productNameCache = PlayerSettings.productName;
            string applicationIdentifierCache = PlayerSettings.applicationIdentifier;
            if (addPackageNameStr != null)
            {
                if (!PlayerSettings.productName.EndsWith(addPackageNameStr))
                {
                    PlayerSettings.productName += addPackageNameStr;
                }
                
                //包名
                if (!PlayerSettings.applicationIdentifier.EndsWith(addPackageNameStr))
                {
                    PlayerSettings.applicationIdentifier += addPackageNameStr;
                }
            }


            //增加平台路径
            var buildRuntimePlatform = BApplication.GetRuntimePlatform(buildTarget);
            var outPlatformDir = IPath.Combine(outdir, BApplication.GetPlatformPath(buildTarget));
            BDFrameworkPipelineHelper.OnBeginBuildPackage(buildTarget, outdir);
            //0.加载场景配置
            Debug.Log("<color=green>===>1.加载场景配置</color>");
            if (!string.IsNullOrEmpty(buildConfig))
            {
                LoadConfig(buildScene, buildConfig);
            }

            //1.生成资源到Devops
            Debug.Log("<color=green>===>2.生成资产</color>");
            if (isGenAssets)
            {
                try
                {
                    BuildAssetsTools.BuildAllAssets(buildRuntimePlatform, BApplication.DevOpsPublishAssetsPath, opa: buildOption);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("提示",$"打包资产失败!","ok");
                    throw e;
                }
            }

            bool buildResult = false;
            string outputpath = "";
            //2.拷贝资源并打包
            AssetDatabase.StartAssetEditing(); //停止触发资源导入
            {
                //拷贝资源
                Debug.Log("<color=green>===>3.拷贝打包资产</color>");
                CopyPublishAssetsTo(Application.streamingAssetsPath, buildRuntimePlatform);
                try
                {
                    Debug.Log("<color=green>===>4.开始构建包体</color>");
                    switch (buildTarget)
                    {
                        case BuildTarget.Android:
                        {
                            (buildResult, outputpath) = BuildAPK(buildMode, outPlatformDir);
                        }
                            break;
                        case BuildTarget.iOS:
                        {
                            (buildResult, outputpath) = BuildIpa(buildMode, outPlatformDir);
                        }
                            break;
                        case BuildTarget.StandaloneWindows:
                        case BuildTarget.StandaloneWindows64:
                        {
                            (buildResult, outputpath) = BuildExe(buildMode, outPlatformDir);
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
                    Debug.LogError($"打包失败!{e}");
                }

                //删除目录
                Directory.Delete(Application.streamingAssetsPath,true);
                //DeleteCopyAssets(Application.streamingAssetsPath, buildRuntimePlatform);
            }
            AssetDatabase.StopAssetEditing(); //恢复触发资源导入

            //恢复包名
            PlayerSettings.productName = productNameCache;
            PlayerSettings.applicationIdentifier = applicationIdentifierCache;
            AssetDatabase.SaveAssets();
            IsBuilding = false;
            //返回构建结果
            return buildResult;
        }


        #region Android

        /// <summary>
        /// 打包APK   
        /// </summary>
        static private (bool, string) BuildAPK(BuildMode mode, string outdir)
        {
            bool ret = false;
            //开启符号表
            EditorUserBuildSettings.androidCreateSymbolsZip = true;

            if (!BDEditorApplication.EditorSetting.IsSetConfig())
            {
                //For ci
                throw new Exception("请注意设置apk keystore账号密码");
            }

            //模式
            AndroidSetting androidConfig = null;
            switch (mode)
            {
                case BuildMode.Debug:
                {
                    androidConfig = BDEditorApplication.EditorSetting.AndroidDebug;
                }
                    break;
                case BuildMode.Release:
                case BuildMode.Profiler:
                {
                    androidConfig = BDEditorApplication.EditorSetting.Android;
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


            var outputPath = IPath.Combine(outdir, string.Format("{0}.apk", Application.identifier));
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
            string[] scenes = { SCENE_PATH };
            BuildOptions opa = BuildOptions.None;
            switch (mode)
            {
                case BuildMode.Debug:
                {
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging |
                          BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
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
            // if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS) == ManagedStrippingcLevel.High)
            // {
            // PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Low);
            //}
            //
            //文件夹处理
            var outputPath = IPath.Combine(outdir, Application.identifier);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            //开始项目一键打包
            string[] scenes = { SCENE_PATH };
            BuildOptions opa = BuildOptions.None;

            switch (mode)
            {
                case BuildMode.Debug:
                {
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging |
                          BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
                case BuildMode.Release:
                {
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }


            var plist = outputPath + "/Info.plist";
            Debug.Log("plist:" + plist);
            //append模式
            if (File.Exists(plist) && Application.platform == RuntimePlatform.OSXEditor)
            {
                opa = (opa | BuildOptions.AcceptExternalModificationsToPlayer);
                Debug.Log("--->生成xcode,depend模式");
            }

            //构建包体
            Debug.Log("------------->Begin build<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.iOS, opa);
            Debug.Log("------------->End build<------------");

            //检测xcode
            if (File.Exists(plist))
            {
                //执行shell path
                var shellPath = mode == BuildMode.Debug
                    ? BDEditorApplication.EditorSetting.iOSDebug.ExcuteShell
                    : BDEditorApplication.EditorSetting.iOS.ExcuteShell;
                if (File.Exists(shellPath))
                {
                    //执行BuildIpa的shell
                    Debug.Log("即将执行:" + shellPath);
                    CMDTools.RunCmdFile(shellPath);

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
                    throw new Exception($"没找到编译shell/cmd脚本: {shellPath}! 后续请配合Jekins/Teamcity出包!");
                }

                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                //For ci
                throw new Exception("【BDFramework】Package not exsit！ - " + plist);
            }

            return (ret, outputPath);
        }

        #endregion

        #region Windows

        /// <summary>
        /// 编译Xcode（这里是出母包版本）
        /// </summary>
        /// <param name="mode"></param>
        static private (bool, string) BuildExe(BuildMode mode, string outdir)
        {
            bool ret = false;
            BDEditorApplication.SwitchToWindows();
            //DeleteIL2cppCache();
            PlayerSettings.gcIncremental = true;
            var outputPath = IPath.Combine(outdir,
                string.Format("{0}_{1}.exe", Application.identifier, mode.ToString()));
            //文件夹处理
            if (Directory.Exists(outdir))
            {
                Directory.Delete(outdir);
            }

            Directory.CreateDirectory(outdir);


            //开始项目一键打包
            string[] scenes = { SCENE_PATH };
            BuildOptions opa = BuildOptions.None;
            switch (mode)
            {
                case BuildMode.Debug:
                {
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging |
                          BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;

                case BuildMode.Release:
                {
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }

            //构建包体
            Debug.Log("------------->Begin build<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.StandaloneWindows64, opa);
            Debug.Log("------------->End build<------------");


            //检测xcode
            if (File.Exists(outputPath))
            {
                Debug.Log("打包Exe成功~");
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


        #region 资产操作类

        /// <summary>
        /// 拷贝发布资源
        /// </summary>
        static public void CopyPublishAssetsTo(string targetpath, RuntimePlatform platform)
        {
            List<string> blackFile = new List<string>()
            {
                BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH, //editor信息
                BResources.ASSETS_INFO_PATH,
                BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH,
                BResources.SERVER_ASSETS_VERSION_INFO_PATH,
                BResources.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH,
                BResources.SBPBuildLog,
                BResources.SBPBuildLog2,
                ".manifest"
            };
            //清空目标文件夹
            if (Directory.Exists(targetpath))
            {
                Directory.Delete(targetpath, true);
            }

            //合并路径
            var sourcepath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformPath(platform))
                .ToLower();
            targetpath = IPath.Combine(targetpath, BApplication.GetPlatformPath(platform)).ToLower();
            //TODO SVN更新资源

            //TODO  重写拷贝逻辑
            var files = Directory.GetFiles(sourcepath, "*", SearchOption.AllDirectories)
                .Select((f) => f.ToLower().Replace("\\", "/"));
            foreach (var file in files)
            {
                var fp = IPath.ReplaceBackSlash(file);
                var ret = blackFile.Find((blackstr) =>
                {
                    //后缀名
                    if (blackstr.StartsWith("."))
                    {
                        return fp.EndsWith(blackstr, StringComparison.OrdinalIgnoreCase);
                    }
                    //路径
                    else
                    {
                        return fp.EndsWith("/" + blackstr, StringComparison.OrdinalIgnoreCase);
                    }
                });
                if (ret != null)
                {
                    Debug.Log("[黑名单]" + fp);
                    continue;
                }

                //
                var tp = fp.Replace(sourcepath, targetpath);

                //拷贝资产,比较hash,最多尝试5次
                int maxTryCount = 5;
                for (int i = 0; i < maxTryCount; i++)
                {
                    FileHelper.Copy(fp, tp, true);
                    var sourceHash = FileHelper.GetMurmurHash3(sourcepath);
                    var targetHash = FileHelper.GetMurmurHash3(targetpath);
                    if (sourceHash == targetHash)
                    {
                        break;
                    }
                    else if (i == maxTryCount - 1)
                    {
                        Debug.LogError("hash不一致，请检查!");
                    }
                }
            }
        }

        /// <summary>
        /// 删除拷贝的资源
        /// </summary>
        /// <param name="targetpath"></param>
        /// <param name="platform"></param>
        static public void DeleteCopyAssets(string targetpath, RuntimePlatform platform)
        {
            targetpath = IPath.Combine(targetpath, BApplication.GetPlatformPath(platform));
            //优先删除拷贝的美术资源，防止构建完再导入  其他资源等工作流完全切入DevOps再进行删除
            var copyArtPath = IPath.Combine(targetpath, BResources.ART_ASSET_ROOT_PATH);
            if (Directory.Exists(copyArtPath))
            {
                Directory.Delete(copyArtPath, true);
            }
        }

        #endregion
    }
}