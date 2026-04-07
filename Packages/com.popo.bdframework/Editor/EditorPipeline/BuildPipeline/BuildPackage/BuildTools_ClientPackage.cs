using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.Editor.Environment;
using BDFramework.Editor.HotfixScript;
using BDFramework.Editor.Inspector.Config;
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
    static public class BuildTools_ClientPackage
    {
        public const string DefaultClientVersion = "0.1.0";
        const string IOSPostBuildShellRelativePath = "DevOps/CI/BuildTools/BuildClientPackage/build_xcode.shell";

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
        static BuildTools_ClientPackage()
        {
            //初始化框架编辑器下
            BDFrameworkEditorEnvironment.InitEditorEnvironment();
        }


        /// <summary>
        /// 加载场景上的配置
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="buildScene"></param>
        sealed class BuildConfigOverrideContext
        {
            public string ConfigPath { get; }
            public string OriginalContent { get; }

            public BuildConfigOverrideContext(string configPath, string originalContent)
            {
                this.ConfigPath = configPath;
                this.OriginalContent = originalContent;
            }

            public void Restore()
            {
                if (!string.IsNullOrEmpty(this.ConfigPath))
                {
                    FileHelper.WriteAllText(this.ConfigPath, this.OriginalContent);
                    AssetDatabase.Refresh();
                }
            }
        }

        sealed class BuildPlayerSettingsScope : IDisposable
        {
            readonly bool previousDevelopment;
            readonly bool previousAllowDebugging;
            readonly bool previousConnectProfiler;
            readonly bool previousDeepProfilingSupport;

            public BuildPlayerSettingsScope(BuildMode buildMode)
            {
                this.previousDevelopment = EditorUserBuildSettings.development;
                this.previousAllowDebugging = EditorUserBuildSettings.allowDebugging;
                this.previousConnectProfiler = EditorUserBuildSettings.connectProfiler;
                this.previousDeepProfilingSupport = EditorUserBuildSettings.buildWithDeepProfilingSupport;

                var isDebugBuild = buildMode == BuildMode.Debug;
                EditorUserBuildSettings.development = isDebugBuild;
                EditorUserBuildSettings.allowDebugging = isDebugBuild;
                EditorUserBuildSettings.connectProfiler = isDebugBuild;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = isDebugBuild;

                Debug.Log($"【BuildPackage】 同步 EditorUserBuildSettings => mode:{buildMode} development:{EditorUserBuildSettings.development} allowDebugging:{EditorUserBuildSettings.allowDebugging} connectProfiler:{EditorUserBuildSettings.connectProfiler} deepProfiling:{EditorUserBuildSettings.buildWithDeepProfilingSupport}");
            }

            public void Dispose()
            {
                EditorUserBuildSettings.development = this.previousDevelopment;
                EditorUserBuildSettings.allowDebugging = this.previousAllowDebugging;
                EditorUserBuildSettings.connectProfiler = this.previousConnectProfiler;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = this.previousDeepProfilingSupport;
            }
        }

        static BuildTargetGroup ResolveBuildTargetGroup(BuildTarget buildTarget)
        {
            var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (buildTargetGroup == BuildTargetGroup.Unknown)
            {
                throw new Exception("未知的构建目标组:" + buildTarget);
            }

            return buildTargetGroup;
        }

        static void EnsureActiveBuildTarget(BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.activeBuildTarget == buildTarget)
            {
                return;
            }

            var buildTargetGroup = ResolveBuildTargetGroup(buildTarget);
            if (!BDEditorApplication.IsPlatformModuleInstalled(buildTargetGroup, buildTarget))
            {
                throw new Exception($"未安装目标平台模块: {buildTarget}");
            }

            Debug.Log($"【BuildPackage】 切换构建平台: {EditorUserBuildSettings.activeBuildTarget} => {buildTarget}");
            var switched = BDEditorApplication.SwitchToBuildTarget(buildTarget);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!switched || EditorUserBuildSettings.activeBuildTarget != buildTarget)
            {
                throw new Exception($"切换目标平台失败: {buildTarget}");
            }
        }

        /// <summary>
        /// 获取默认母包版本号
        /// </summary>
        public static string GetDefaultClientVersion()
        {
            var clientVersion = BDEditorApplication.EditorSetting?.BuildClientPackage?.ClientVersion;
            if (!string.IsNullOrWhiteSpace(clientVersion))
            {
                return clientVersion.Trim();
            }

            try
            {
                var config = ConfigEditorUtil.GetEditorConfig<GameBaseConfigProcessor.Config>();
                if (!string.IsNullOrWhiteSpace(config?.ClientVersionNum))
                {
                    return config.ClientVersionNum.Trim();
                }
            }
            catch
            {
            }

            return DefaultClientVersion;
        }

        static string NormalizeClientVersion(string clientVersion)
        {
            return string.IsNullOrWhiteSpace(clientVersion) ? GetDefaultClientVersion() : clientVersion.Trim();
        }

        static string GetIOSPostBuildShellPath()
        {
            return IPath.Combine(BApplication.ProjectRoot, IOSPostBuildShellRelativePath);
        }

        static string BuildIOSPostBuildShellArgs(string xcodeProjectDir, BuildMode mode)
        {
            var configuration = mode == BuildMode.Debug ? "Debug" : "Release";
            return $"--project-dir \"{xcodeProjectDir}\" --configuration {configuration}";
        }

        static BuildConfigOverrideContext OverrideBuildConfigClientVersion(string buildConfigPath, string clientVersion)
        {
            if (string.IsNullOrEmpty(buildConfigPath) || string.IsNullOrEmpty(clientVersion) || !File.Exists(buildConfigPath))
            {
                return null;
            }

            var originalContent = File.ReadAllText(buildConfigPath);
            var configList = GameConfigManager.Inst.LoadConfig(originalContent).Item1;
            var baseConfig = configList.OfType<GameBaseConfigProcessor.Config>().FirstOrDefault();
            if (baseConfig == null || string.Equals(baseConfig.ClientVersionNum, clientVersion, StringComparison.Ordinal))
            {
                return null;
            }

            baseConfig.ClientVersionNum = clientVersion;
            ConfigEditorUtil.SaveConfig(buildConfigPath, configList);
            AssetDatabase.Refresh();
            Debug.Log($"【BuildPackage】 覆盖母包配置版本号: {buildConfigPath} => {clientVersion}");
            return new BuildConfigOverrideContext(buildConfigPath, originalContent);
        }

        /// <summary>
        /// 加载场景上的配置
        /// </summary>
        static BuildConfigOverrideContext LoadConfig(string buildScene, string buildConfig, string clientVersion)
        {
            var scene = EditorSceneManager.OpenScene(buildScene);
            var launcher = GameObject.FindObjectOfType<BDLauncher>();
            if (launcher == null)
            {
                throw new Exception($"【BuildPackage】 场景中未找到BDLauncher: {buildScene}");
            }

            var resolvedBuildConfig = buildConfig;
            if (string.IsNullOrEmpty(resolvedBuildConfig) && launcher.ConfigText != null)
            {
                resolvedBuildConfig = AssetDatabase.GetAssetPath(launcher.ConfigText);
            }

            var overrideContext = OverrideBuildConfigClientVersion(resolvedBuildConfig, clientVersion);
            if (!string.IsNullOrEmpty(resolvedBuildConfig))
            {
                var textContent = AssetDatabase.LoadAssetAtPath<TextAsset>(resolvedBuildConfig);
                if (textContent == null)
                {
                    throw new Exception($"【BuildPackage】 未找到构建配置: {resolvedBuildConfig}");
                }

                launcher.ConfigText = textContent;
                Debug.LogFormat("【BuildPackage】 加载配置:{0} \n {1}", resolvedBuildConfig, launcher.ConfigText);
                GameConfigManager.Inst.LoadConfig(launcher.ConfigText.text);
            }
            launcher.ClientVersion = clientVersion;
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
            return overrideContext;
        }

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// 这里默认建议使用单场景结构打包
        /// </summary>
        static public bool Build(BuildMode buildMode, bool isGenAssets, string outdir, BuildTarget buildTarget, BuildTools_Assets.BuildPackageOption buildOption = BuildTools_Assets.BuildPackageOption.BuildAll, string clientVersion = null)
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
            return Build(buildMode, SCENE_PATH, buildConfig, isGenAssets, outdir, buildTarget, buildOption, clientVersion);
        }

        static public bool IsBuilding { get; private set; } = false;

        /// <summary>
        /// 构建包体，使用当前配置、资源
        /// 这里默认建议使用单场景结构打包.
        /// </summary>
        static public bool Build(BuildMode buildMode, string buildScene, string buildConfig, bool isGenAssets, string outdir, BuildTarget buildTarget, BuildTools_Assets.BuildPackageOption buildOption = BuildTools_Assets.BuildPackageOption.BuildAll, string clientVersion = null)
        {
            BDebug.Log("=========>开始构建母包流程<==========", Color.yellow );
            if (IsBuilding)
            {
                return false;
            }

            IsBuilding = true;
            clientVersion = NormalizeClientVersion(clientVersion);
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
            EnsureActiveBuildTarget(buildTarget);
            var buildTargetGroup = ResolveBuildTargetGroup(buildTarget);

            //不通模式的设置
            //项目名

            #region 容错

            if (PlayerSettings.productName.EndsWith(".Debug") || PlayerSettings.productName.EndsWith(".debug"))
            {
                PlayerSettings.productName = PlayerSettings.productName.Substring(0, PlayerSettings.productName.Length - 6);
            }

            if (PlayerSettings.productName.EndsWith(".Profiler") || PlayerSettings.productName.EndsWith(".profiler"))
            {
                PlayerSettings.productName = PlayerSettings.productName.Substring(0, PlayerSettings.productName.Length - 9);
            }

            //
            var applicationIdentifier = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);
            if (applicationIdentifier.EndsWith(".Debug") || applicationIdentifier.EndsWith(".debug"))
            {
                applicationIdentifier = applicationIdentifier.Substring(0, applicationIdentifier.Length - 6);
            }

            if (applicationIdentifier.EndsWith(".Profiler") || applicationIdentifier.EndsWith(".profiler"))
            {
                applicationIdentifier = applicationIdentifier.Substring(0, applicationIdentifier.Length - 9);
            }

            #endregion

            string productNameCache = PlayerSettings.productName;
            string applicationIdentifierCache = applicationIdentifier;
            if (addPackageNameStr != null)
            {
                if (!PlayerSettings.productName.EndsWith(addPackageNameStr))
                {
                    PlayerSettings.productName += addPackageNameStr;
                }

                //包名
                if (!applicationIdentifier.EndsWith(addPackageNameStr))
                {
                    applicationIdentifier += addPackageNameStr;
                }
            }

            PlayerSettings.SetApplicationIdentifier(buildTargetGroup, applicationIdentifier);


            //增加平台路径
            var buildRuntimePlatform = BApplication.GetRuntimePlatform(buildTarget);
            var outPlatformDir = IPath.Combine(outdir, BApplication.GetPlatformPath(buildTarget));
            BuildConfigOverrideContext configOverrideContext = null;
            BuildPlayerSettingsScope buildPlayerSettingsScope = null;
            var isAssetEditing = false;
            bool buildResult = false;
            string outputpath = "";

            try
            {
                BDFrameworkPipelineHelper.OnBeginBuildPackage(buildTarget, outPlatformDir, clientVersion);

                //0.加载场景配置
                BDebug.Log($"===>1.加载场景配置(母包版本:{clientVersion})", Color.yellow );
                configOverrideContext = LoadConfig(buildScene, buildConfig, clientVersion);
                buildPlayerSettingsScope = new BuildPlayerSettingsScope(buildMode);

#if ENABLE_HYCLR
                BDebug.Log("===>开始处理华佗", Color.magenta );
                HyCLREditorTools.PreBuild(buildTarget);
#endif
                //1.生成资源到Devops
                BDebug.Log("===>2.生成资产", Color.yellow );
                var assetOutputPath = BApplication.DevOpsPublishAssetsPath;
                if (isGenAssets)
                {
                    try
                    {
                        BuildTools_Assets.BuildAll(buildRuntimePlatform, assetOutputPath, opa: buildOption);
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("提示", $"打包资产失败!", "ok");
                        throw e;
                    }
                }

                //2.拷贝资源并打包
                AssetDatabase.StartAssetEditing();
                isAssetEditing = true;
                //拷贝资源
                BDebug.Log("===>3.拷贝打包资产: DevopsPublish => streamingAssetsPath", Color.yellow );
                CopyDevopsPublishAssetsTo(Application.streamingAssetsPath, buildRuntimePlatform);
                try
                {
                    BDebug.Log("===>4.开始构建包体", Color.yellow );
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
                    BDebug.Log("===>5.构建结束", Color.yellow );
                }
                catch (Exception e)
                {
                    Debug.LogError($"打包失败!{e}");
                }

                //删除目录
                BDebug.Log("=========>构建母包结束,开始清理<==========", Color.yellow );
                if (Directory.Exists(Application.streamingAssetsPath))
                {
                    Directory.Delete(Application.streamingAssetsPath, true);
                }
            }
            finally
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                }

                configOverrideContext?.Restore();
                buildPlayerSettingsScope?.Dispose();
                PlayerSettings.productName = productNameCache;
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup, applicationIdentifierCache);
                AssetDatabase.SaveAssets();
                IsBuilding = false;
            }

            //返回构建结果
            return buildResult;
        }


        #region Android

        /// <summary>
        /// 打包APK   
        /// </summary>
        static private (bool, string) BuildAPK(BuildMode mode, string outdir)
        {
            var baseConfig = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            //
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

            if (baseConfig.CodeRoot != AssetLoadPathType.Editor && baseConfig.CodeRunMode == HotfixCodeRunMode.HyCLR)
            {
                PlayerSettings.gcIncremental = false;
                PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_4_6);
            }
            else
            {
                PlayerSettings.gcIncremental = true;
            }

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
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                }
                    break;
                case BuildMode.Release:
                {
                    opa = BuildOptions.CompressWithLz4HC;
                }
                    break;
            }


            //构建包体
            Debug.Log("------------->Begin build game client:<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, opa);
            Debug.Log("------------->End build game client!<------------");

            //构建出判断
            if (File.Exists(outputPath))
            {
                Debug.Log("Build Success :" + outputPath);
                ret = true;
                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(outputPath);
                }
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
            var baseConfig = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            bool ret = false;
            BDEditorApplication.SwitchToiOS();
            //DeleteIL2cppCache();
            //具体IOS的的配置
            if (baseConfig.CodeRoot != AssetLoadPathType.Editor && baseConfig.CodeRunMode == HotfixCodeRunMode.HyCLR)
            {
                PlayerSettings.gcIncremental = false;
                PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_4_6);
            }
            else
            {
                PlayerSettings.gcIncremental = true;
            }

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
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
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
            
#if ENABLE_HYCLR
            //HCLR 需要保存
            // var libil2cppPath = IPath.Combine(outputPath,"Libraries");
            // HCLREditorTools.CopyLibIl2cppToXcode(libil2cppPath);
#endif
            Debug.Log("------------->End build<------------");

            //检测xcode
            if (File.Exists(plist))
            {
                ret = true;

                var shellPath = GetIOSPostBuildShellPath();
                if (File.Exists(shellPath))
                {
                    if (CMDTools.CanRunCmdFile(shellPath))
                    {
                        var shellArgs = BuildIOSPostBuildShellArgs(outputPath, mode);
                        Debug.Log($"即将执行: {shellPath} {shellArgs}");
                        var shellExitCode = CMDTools.RunCmdFile(shellPath, shellArgs);
                        if (shellExitCode != 0)
                        {
                            throw new Exception($"iOS 后置脚本执行失败，exitCode={shellExitCode} path={shellPath} args={shellArgs}");
                        }

                        var ipaPath = outputPath + ".ipa";
                        if (File.Exists(ipaPath))
                        {
                            outputPath = ipaPath;
                        }
                        else
                        {
                            Debug.LogWarning($"iOS 后置脚本未生成 ipa，保留 Xcode 工程输出: {outputPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"当前宿主 {Application.platform} 不支持直接执行 iOS 后置脚本 {shellPath}，保留 Xcode 工程输出: {outputPath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"未找到 iOS 后置脚本: {shellPath}，保留 Xcode 工程输出: {outputPath}");
                }

                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(outputPath);
                }
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

            var baseConfig = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            bool ret = false;
            BDEditorApplication.SwitchToWindows();
            //DeleteIL2cppCache();
            if (baseConfig.CodeRoot != AssetLoadPathType.Editor && baseConfig.CodeRunMode == HotfixCodeRunMode.HyCLR)
            {
                PlayerSettings.gcIncremental = false;
                PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
            }
            else
            {
                PlayerSettings.gcIncremental = true;
            }

            outdir = IPath.Combine(outdir, Application.identifier);
            var outputPath = IPath.Combine(outdir, "Launcher.exe");
            //文件夹处理
            if (Directory.Exists(outdir))
            {
                Directory.Delete(outdir, true);
            }

            Directory.CreateDirectory(outdir);


            //开始项目一键打包
            string[] scenes = { SCENE_PATH };
            BuildOptions opa = BuildOptions.None;
            switch (mode)
            {
                case BuildMode.Debug:
                {
                    opa = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
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
                ret = true;
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
        

        #region 资产操作类

        /// <summary>
        /// 拷贝发布资源
        /// 这里注意不要传BApplication.Streaming
        /// </summary>
        static public void CopyDevopsPublishAssetsTo(string targetpath, RuntimePlatform platform)
        {
            List<string> blackFile = new List<string>()
            {
                BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH, //editor信息
                BResources.ASSETS_INFO_PATH, BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH,
                BResources.SERVER_ASSETS_VERSION_INFO_PATH, BResources.SERVER_ASSETS_SUB_PACKAGE_INFO_PATH,
                BResources.SBPBuildLog, BResources.SBPBuildLog2, ".manifest"
            };
            //清空目标文件夹
            if (Directory.Exists(targetpath))
            {
                Directory.Delete(targetpath, true);
            }

            //合并路径
            var sourcepath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformLoadPath(platform)).ToLower();
            targetpath = IPath.Combine(targetpath, BApplication.GetPlatformLoadPath(platform)).ToLower();
            var files = Directory.GetFiles(sourcepath, "*", SearchOption.AllDirectories).Select((f) => f.ToLower().Replace("\\", "/"));
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
            targetpath = IPath.Combine(targetpath, BApplication.GetPlatformLoadPath(platform));
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