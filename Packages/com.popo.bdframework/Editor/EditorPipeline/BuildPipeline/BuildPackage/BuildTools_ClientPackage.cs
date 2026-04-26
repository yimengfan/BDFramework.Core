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
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.BuildPipeline
{
    /// <summary>
    /// 构建包体工具。
    /// Package build tool.
    /// 这里负责母包构建主流程，包括配置装载、HybridCLR 预处理、资源拷贝与最终 BuildPlayer 调用。
    /// This type owns the primary package-build flow, including config loading, HybridCLR prebuild, asset copying, and the final BuildPlayer call.
    /// </summary>
    static public class BuildTools_ClientPackage
    {
        public const string DefaultClientVersion = "0.1.0";
        internal const string ClientVersionBatchArgName = "-clientVersion";
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

        /// <summary>
        /// 在正式 BuildPlayer 前暂存并覆盖 EditorUserBuildSettings。
        /// Temporarily capture and override EditorUserBuildSettings before the final BuildPlayer call.
        /// 这里集中控制 development、debugging 与 profiler 相关开关，避免 CI 预处理或 Windows 无头 Player
        /// 在不同调用点各自散落一套调试标记策略。
        /// This keeps development, debugging, and profiler-related flags in one place so CI prebuilds and
        /// Windows headless players do not each drift into separate debug-flag policies.
        /// </summary>
        sealed class BuildPlayerSettingsScope : IDisposable
        {
            readonly bool previousDevelopment;
            readonly bool previousAllowDebugging;
            readonly bool previousConnectProfiler;
            readonly bool previousDeepProfilingSupport;

            /// <summary>
            /// 根据目标平台与构建模式覆盖 EditorUserBuildSettings。
            /// Override EditorUserBuildSettings according to the target platform and package build mode.
            /// Windows Talos 调试母包必须保留脚本调试能力，但不能再自动连接 profiler，
            /// 否则无头 TeamCity agent 上的 Standalone Player 会在进入托管启动前卡住。
            /// Windows Talos debug packages must keep script debugging, but they must no longer auto-connect the profiler,
            /// otherwise the standalone player can stall on headless TeamCity agents before managed startup begins.
            /// 窗口分辨率、全屏模式与 resizableWindow 不再由本作用域强制覆盖，
            /// 统一由 Unity ProjectSettings/ProjectSettings.asset 控制。
            /// Window resolution, fullscreen mode and resizableWindow are no longer overridden by this scope;
            /// they are controlled exclusively via Unity ProjectSettings/ProjectSettings.asset.
            /// </summary>
            public BuildPlayerSettingsScope(BuildMode buildMode, BuildTarget buildTarget)
            {
                this.previousDevelopment = EditorUserBuildSettings.development;
                this.previousAllowDebugging = EditorUserBuildSettings.allowDebugging;
                this.previousConnectProfiler = EditorUserBuildSettings.connectProfiler;
                this.previousDeepProfilingSupport = EditorUserBuildSettings.buildWithDeepProfilingSupport;

                var isDebugBuild = buildMode == BuildMode.Debug;
                var enableProfiler = ShouldEnableProfilerForPackageBuild(buildMode, buildTarget);
                EditorUserBuildSettings.development = isDebugBuild;
                EditorUserBuildSettings.allowDebugging = isDebugBuild;
                EditorUserBuildSettings.connectProfiler = enableProfiler;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = enableProfiler;

                Debug.Log($"【BuildPackage】 同步 EditorUserBuildSettings => target:{buildTarget} mode:{buildMode} development:{EditorUserBuildSettings.development} allowDebugging:{EditorUserBuildSettings.allowDebugging} connectProfiler:{EditorUserBuildSettings.connectProfiler} deepProfiling:{EditorUserBuildSettings.buildWithDeepProfilingSupport}");
            }

            public void Dispose()
            {
                EditorUserBuildSettings.development = this.previousDevelopment;
                EditorUserBuildSettings.allowDebugging = this.previousAllowDebugging;
                EditorUserBuildSettings.connectProfiler = this.previousConnectProfiler;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = this.previousDeepProfilingSupport;
            }
        }

        /// <summary>
        /// 判断当前母包构建是否应该开启 profiler 与 deep profiling。
        /// Determine whether the current package build should enable profiler and deep-profiling flags.
        /// Windows Talos 调试母包在 CI 上只需要脚本调试，不需要 profiler 握手；
        /// 其余 Debug 包继续沿用现有 profiler 行为，避免误改 Android 等已稳定链路。
        /// Windows Talos debug packages on CI only need script debugging and should skip profiler handshakes;
        /// other debug packages keep the existing profiler behavior so stable Android and other flows do not change unexpectedly.
        /// </summary>
        static bool ShouldEnableProfilerForPackageBuild(BuildMode buildMode, BuildTarget buildTarget)
        {
            if (buildMode != BuildMode.Debug)
            {
                return false;
            }

            return buildTarget != BuildTarget.StandaloneWindows64;
        }

        /// <summary>
        /// 计算 Windows Standalone 母包的 BuildOptions。
        /// Resolve the BuildOptions for Windows standalone package builds.
        /// Windows Talos 调试包保留 Development 与 AllowDebugging，
        /// 但显式移除 profiler 与 deep profiling，避免 Player 在无头 agent 上卡在 profiler 连接阶段。
        /// Windows Talos debug packages keep Development and AllowDebugging,
        /// but explicitly drop profiler and deep-profiling flags so the player does not stall on headless agents during profiler connection.
        /// </summary>
        static BuildOptions ResolveWindowsBuildOptions(BuildMode mode)
        {
            switch (mode)
            {
                case BuildMode.Debug:
                {
                    var options = BuildOptions.CompressWithLz4HC | BuildOptions.Development | BuildOptions.AllowDebugging;
                    if (ShouldEnableProfilerForPackageBuild(mode, BuildTarget.StandaloneWindows64))
                    {
                        options |= BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport;
                    }

                    return options;
                }

                case BuildMode.Release:
                {
                    return BuildOptions.CompressWithLz4HC;
                }

                default:
                {
                    return BuildOptions.None;
                }
            }
        }

        /// <summary>
        /// 判断当前母包构建是否必须先执行 HybridCLR 预处理。
        /// 只要项目启用了本地 HybridCLR 且未使用 global il2cpp，就必须在真正 BuildPlayer 前完成准备，
        /// 避免当前仍处于 Editor/Mono 配置时被误判为无需预处理，最终在构建前置检查阶段失败。
        /// </summary>
        public static bool ShouldPrepareHybridClrForPackageBuild(
            bool hybridClrEnabled,
            bool useGlobalIl2cpp,
            ScriptingImplementation scriptingBackend,
            AssetLoadPathType? codeRoot,
            HotfixCodeRunMode? codeRunMode,
            out string reason)
        {
            var codeRootText = codeRoot.HasValue ? codeRoot.Value.ToString() : "<null>";
            var codeRunModeText = codeRunMode.HasValue ? codeRunMode.Value.ToString() : "<null>";

            if (!hybridClrEnabled)
            {
                reason = $"skip CodeRoot={codeRootText} CodeRunMode={codeRunModeText} scriptingBackend={scriptingBackend} hybridClrEnabled=False useGlobalIl2cpp={useGlobalIl2cpp}";
                return false;
            }

            if (useGlobalIl2cpp)
            {
                reason = $"skip CodeRoot={codeRootText} CodeRunMode={codeRunModeText} scriptingBackend={scriptingBackend} hybridClrEnabled=True useGlobalIl2cpp=True";
                return false;
            }

            if (codeRoot.HasValue && codeRunMode.HasValue && codeRoot.Value != AssetLoadPathType.Editor && codeRunMode.Value == HotfixCodeRunMode.HyCLR)
            {
                reason = $"gameConfig CodeRoot={codeRootText} CodeRunMode={codeRunModeText}";
                return true;
            }

            reason = $"hybridClrPackage enabled for package build CodeRoot={codeRootText} CodeRunMode={codeRunModeText} scriptingBackend={scriptingBackend}";
            return true;
        }

        /// <summary>
        /// 根据当前工程配置判断母包构建前是否需要执行 HybridCLR 预处理。
        /// </summary>
        static bool ShouldPrepareHybridClrForBuild(BuildTargetGroup buildTargetGroup, out string reason)
        {
            var baseConfig = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            var scriptingBackend = PlayerSettings.GetScriptingBackend(buildTargetGroup);
            var hybridClrSettings = HybridCLRSettings.Instance;
            return ShouldPrepareHybridClrForPackageBuild(
                hybridClrSettings != null && hybridClrSettings.enable,
                hybridClrSettings != null && hybridClrSettings.useGlobalIl2cpp,
                scriptingBackend,
                baseConfig != null ? (AssetLoadPathType?)baseConfig.CodeRoot : null,
                baseConfig != null ? (HotfixCodeRunMode?)baseConfig.CodeRunMode : null,
                out reason);
        }

        /// <summary>
        /// 先执行 HybridCLR 预处理，再创建正式母包构建阶段需要的 EditorUserBuildSettings 覆盖作用域。
        /// Run HybridCLR prebuild first, then create the EditorUserBuildSettings override scope required by the real package build phase.
        /// HybridCLR 的临时 stripped-AOT 构建依赖稳定的默认 EditorUserBuildSettings；
        /// 如果过早把 Debug 包的 profiler、debugging 与 deep profiling 开关写入全局状态，第三方内部 BuildPlayer 会继承这些调试标记并在 CI 上失败。
        /// HybridCLR's temporary stripped-AOT build depends on stable default EditorUserBuildSettings;
        /// if debug-package profiler, debugging, and deep-profiling flags are written into the global state too early,
        /// the third-party internal BuildPlayer inherits those debug markers and can fail on CI.
        /// </summary>
        /// <param name="buildMode">正式母包构建模式。</param>
        /// <param name="buildMode">Final package build mode.</param>
        /// <param name="shouldPrepareHybridClr">是否需要执行 HybridCLR 预处理。</param>
        /// <param name="shouldPrepareHybridClr">Whether HybridCLR prebuild should run.</param>
        /// <param name="hybridClrPrepareReason">HybridCLR 预处理判定原因。</param>
        /// <param name="hybridClrPrepareReason">Reason describing the HybridCLR prebuild decision.</param>
        /// <param name="buildTarget">目标平台。</param>
        /// <param name="buildTarget">Target platform.</param>
        /// <param name="hybridClrPreBuildAction">可选的 HybridCLR 预处理委托，默认调用 <c>HyCLREditorTools.PreBuild</c>。</param>
        /// <param name="hybridClrPreBuildAction">Optional HybridCLR prebuild delegate, defaulting to <c>HyCLREditorTools.PreBuild</c>.</param>
        /// <returns>供正式母包构建阶段使用的 EditorUserBuildSettings 作用域。</returns>
        /// <returns>An EditorUserBuildSettings scope for the final package build phase.</returns>
        static IDisposable PrepareHybridClrAndCreateBuildPlayerSettingsScope(
            BuildMode buildMode,
            bool shouldPrepareHybridClr,
            string hybridClrPrepareReason,
            BuildTarget buildTarget,
            Action<BuildTarget> hybridClrPreBuildAction = null)
        {
            Debug.Log($"【BuildPackage】 HybridCLR 预处理判定 => shouldPrepare:{shouldPrepareHybridClr} reason:{hybridClrPrepareReason}");
            if (shouldPrepareHybridClr)
            {
                Debug.Log($"【BuildPackage】 开始执行 HybridCLR 预处理: {hybridClrPrepareReason}");
                (hybridClrPreBuildAction ?? HyCLREditorTools.PreBuild)(buildTarget);
                Debug.Log("【BuildPackage】 HybridCLR 预处理完成");
            }

            return new BuildPlayerSettingsScope(buildMode, buildTarget);
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

        /// <summary>
        /// 解析本次 BatchMode 母包构建应使用的 clientVersion。
        /// 如果命令行没有显式传入，就回退到 BuildTools_ClientPackage 的默认版本号规则。
        /// </summary>
        public static string GetClientVersionForBatchMode()
        {
            var clientVersion = BatchModeCommandLine.GetArg(ClientVersionBatchArgName);
            if (!string.IsNullOrWhiteSpace(clientVersion))
            {
                return clientVersion.Trim();
            }

            return GetDefaultClientVersion();
        }

        /// <summary>
        /// 执行母包 BatchMode 构建入口。
        /// 这里负责收敛版本号解析、Android External Tools 补齐和失败契约，真正的打包仍复用现有 Build 主流程。
        /// </summary>
        public static void BuildClientPackageForBatchMode(BuildTarget buildTarget, BuildMode buildMode)
        {
            var clientVersion = GetClientVersionForBatchMode();
            Debug.Log($"【CI】BuildTarget:{buildTarget} BuildMode:{buildMode} ClientVersion:{clientVersion}");

            if (buildTarget == BuildTarget.Android)
            {
                AndroidExternalToolsBatchResolver.EnsureAndroidExternalToolsForBatchMode();
            }

            var ret = Build(
                buildMode,
                true,
                BApplication.DevOpsPublishClientPackagePath,
                buildTarget,
                BuildTools_Assets.BuildPackageOption.BuildAll,
                clientVersion);

            if (!ret)
            {
                throw new Exception($"【CI】构建母包失败! Target:{buildTarget} Mode:{buildMode} ClientVersion:{clientVersion}");
            }
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
        /// 构建包体，使用当前配置、资源。
        /// Build the package with the current config and assets.
        /// 这里默认建议使用单场景结构打包。
        /// This flow assumes the single-scene package-build layout by default.
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
            IDisposable buildPlayerSettingsScope = null;
            var isAssetEditing = false;
            bool buildResult = false;
            string outputpath = "";

            try
            {
                BDFrameworkPipelineHelper.OnBeginBuildPackage(buildTarget, outPlatformDir, clientVersion);

                //0.加载场景配置
                BDebug.Log($"===>1.加载场景配置(母包版本:{clientVersion})", Color.yellow );
                configOverrideContext = LoadConfig(buildScene, buildConfig, clientVersion);
                var shouldPrepareHybridClr = ShouldPrepareHybridClrForBuild(buildTargetGroup, out var hybridClrPrepareReason);
                buildPlayerSettingsScope = PrepareHybridClrAndCreateBuildPlayerSettingsScope(
                    buildMode,
                    shouldPrepareHybridClr,
                    hybridClrPrepareReason,
                    buildTarget);
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
        /// 构建 Windows Standalone 母包并返回可执行文件路径。
        /// Build the Windows standalone package and return the executable path.
        /// 这里复用统一的场景与资源装配流程，但会对 Windows 调试包额外收紧 profiler 相关标记，
        /// 避免 TeamCity 无头 agent 上的 Talos Player 在进入托管启动前被 profiler 握手阻塞。
        /// This reuses the shared scene and asset packaging flow, but further tightens profiler-related flags for Windows debug packages
        /// so Talos players on headless TeamCity agents are not blocked by profiler handshakes before managed startup.
        /// </summary>
        /// <param name="mode">母包构建模式。</param>
        /// <param name="mode">Package build mode.</param>
        /// <param name="outdir">Windows 输出目录。</param>
        /// <param name="outdir">Windows output directory.</param>
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
            var opa = ResolveWindowsBuildOptions(mode);
            Debug.Log($"【BuildPackage】 Windows BuildOptions => mode:{mode} options:{opa}");

            //构建包体
            Debug.Log("------------->Begin build<------------");
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.StandaloneWindows64, opa);
            Debug.Log("------------->End build<------------");


            //检测xcode
            if (File.Exists(outputPath))
            {
                EnsureHybridClrHotUpdateAssembliesCopiedToManaged(outputPath);
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
        /// 确保 Player 的 Managed 目录中存在所有 HybridCLR 热更 DLL。
        /// Ensure that the Player Managed directory contains every HybridCLR hot-update DLL.
        /// Unity 2021 之后的构建后处理只会把热更程序集名字补进 <c>ScriptingAssemblies.json</c>，
        /// 但首场景里的热更 MonoBehaviour 反序列化仍依赖启动时能在 Managed 目录里找到对应 DLL。
        /// 如果这里只留 StreamingAssets 里的 <c>.zlua.bytes</c>，Unity 会先把首场景脚本降级成 missing script，
        /// 后续再通过 Assembly.Load 手工装载也无法恢复已经丢失的组件实例。
        /// Newer Unity post-build processing only patches hot-update assembly names into <c>ScriptingAssemblies.json</c>,
        /// but first-scene hotfix MonoBehaviour deserialization still depends on finding the corresponding DLLs under the Managed directory at startup.
        /// If the package keeps only <c>.zlua.bytes</c> under StreamingAssets, Unity degrades those startup-scene scripts into missing-script placeholders first,
        /// and a later manual Assembly.Load cannot restore the already-lost component instances.
        /// </summary>
        /// <param name="playerOutputPath">Player 输出路径，例如 Windows 的 Launcher.exe。</param>
        /// <param name="playerOutputPath">Player output path, such as Launcher.exe on Windows.</param>
        static private void EnsureHybridClrHotUpdateAssembliesCopiedToManaged(string playerOutputPath)
        {
            var allConfiguredHotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (allConfiguredHotUpdateAssemblies.Length == 0)
            {
                Debug.Log("[BuildPackage] 当前没有可补齐到 Managed 的 HybridCLR 热更程序集");
                return;
            }

            var nonPreservedHotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var preservedHotUpdateAssemblies = allConfiguredHotUpdateAssemblies
                .Except(nonPreservedHotUpdateAssemblies, StringComparer.Ordinal)
                .ToArray();
            var scriptAssembliesRoot = Path.GetFullPath(Path.Combine(BApplication.ProjectRoot, "Library", "ScriptAssemblies"));
            CopyHybridClrHotUpdateAssembliesToManagedDirectory(playerOutputPath, nonPreservedHotUpdateAssemblies, scriptAssembliesRoot);
            EnsurePreservedHybridClrHotUpdateAssembliesExistInManaged(playerOutputPath, preservedHotUpdateAssemblies, scriptAssembliesRoot);
        }

        /// <summary>
        /// 把指定热更程序集从 ScriptAssemblies 复制到 Player Managed 目录。
        /// Copy the specified hot-update assemblies from ScriptAssemblies into the Player Managed directory.
        /// 该 helper 同时服务真实构建与编辑器契约测试，保证 Windows 母包首场景里引用的热更脚本
        /// 不会因为 Managed 目录缺少 DLL 实体而在 Unity 启动阶段变成 missing script。
        /// This helper serves both real builds and editor contract tests so hotfix scripts referenced by the Windows startup scene
        /// do not regress into missing scripts just because the Player Managed directory lacks physical DLL files.
        /// </summary>
        /// <param name="playerOutputPath">Player 输出路径，例如 Launcher.exe。</param>
        /// <param name="playerOutputPath">Player output path, for example Launcher.exe.</param>
        /// <param name="hotUpdateAssemblies">要补齐的热更程序集短名列表。</param>
        /// <param name="hotUpdateAssemblies">Short names of hot-update assemblies that must be copied.</param>
        /// <param name="scriptAssembliesRoot">Unity 编译后 ScriptAssemblies 目录。</param>
        /// <param name="scriptAssembliesRoot">Unity ScriptAssemblies output directory.</param>
        static private void CopyHybridClrHotUpdateAssembliesToManagedDirectory(
            string playerOutputPath,
            IEnumerable<string> hotUpdateAssemblies,
            string scriptAssembliesRoot)
        {
            if (string.IsNullOrWhiteSpace(playerOutputPath))
            {
                throw new ArgumentException("Player 输出路径不能为空。", nameof(playerOutputPath));
            }

            if (hotUpdateAssemblies == null)
            {
                throw new ArgumentNullException(nameof(hotUpdateAssemblies));
            }

            if (string.IsNullOrWhiteSpace(scriptAssembliesRoot))
            {
                throw new ArgumentException("ScriptAssemblies 目录不能为空。", nameof(scriptAssembliesRoot));
            }

            var managedDirectory = ResolveManagedDirectoryForPlayer(playerOutputPath);
            Directory.CreateDirectory(managedDirectory);

            foreach (var assemblyName in hotUpdateAssemblies.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal))
            {
                var sourcePath = Path.Combine(scriptAssembliesRoot, $"{assemblyName}.dll");
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"缺少热更程序集源文件: {sourcePath}", sourcePath);
                }

                var destinationPath = Path.Combine(managedDirectory, $"{assemblyName}.dll");
                FileHelper.Copy(sourcePath, destinationPath, true);
                Debug.Log($"[BuildPackage] 已补齐 HybridCLR 热更 DLL 到 Managed: {sourcePath} => {destinationPath}");
            }
        }

        /// <summary>
        /// 确保 preserved 热更程序集至少在 Managed 目录中存在一份可解析的 DLL 实体。
        /// Ensure preserved hot-update assemblies still have a resolvable DLL present in the Managed directory.
        /// 某些 Windows 构建链路虽然声明了 preserved hot update assemblies，
        /// 但实际产物仍可能缺少对应的 Managed DLL，导致首场景脚本在 Unity 启动时直接退化成 missing script。
        /// Some Windows build paths declare preserved hot-update assemblies,
        /// but the actual player output can still miss the matching Managed DLL and cause startup-scene scripts to degrade into missing scripts during Unity boot.
        /// 这里仅在目标 DLL 缺失时补齐，不覆盖 Player 已自带的 preserved 程序集副本。
        /// This only backfills missing DLLs and does not overwrite preserved assemblies already shipped with the player.
        /// </summary>
        static private void EnsurePreservedHybridClrHotUpdateAssembliesExistInManaged(
            string playerOutputPath,
            IEnumerable<string> preservedHotUpdateAssemblies,
            string scriptAssembliesRoot)
        {
            if (preservedHotUpdateAssemblies == null)
            {
                return;
            }

            var managedDirectory = ResolveManagedDirectoryForPlayer(playerOutputPath);
            Directory.CreateDirectory(managedDirectory);

            foreach (var assemblyName in preservedHotUpdateAssemblies.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal))
            {
                var destinationPath = Path.Combine(managedDirectory, $"{assemblyName}.dll");
                if (File.Exists(destinationPath))
                {
                    Debug.Log($"[BuildPackage] preserved HybridCLR 热更 DLL 已存在，跳过回填: {destinationPath}");
                    continue;
                }

                var sourcePath = Path.Combine(scriptAssembliesRoot, $"{assemblyName}.dll");
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"缺少 preserved 热更程序集源文件: {sourcePath}", sourcePath);
                }

                FileHelper.Copy(sourcePath, destinationPath, true);
                Debug.Log($"[BuildPackage] 已回填 preserved HybridCLR 热更 DLL 到 Managed: {sourcePath} => {destinationPath}");
            }
        }

        /// <summary>
        /// 根据 Player 输出路径解析其 Managed 目录。
        /// Resolve the Managed directory from a Player output path.
        /// 当前主用场景是 Windows Standalone 输出的 <c>Launcher.exe</c>，
        /// 因此这里按 Unity 约定推导出同级的 <c>&lt;PlayerName&gt;_Data/Managed</c>。
        /// The primary current scenario is a Windows standalone output like <c>Launcher.exe</c>,
        /// so this resolves the sibling <c>&lt;PlayerName&gt;_Data/Managed</c> directory following Unity's output convention.
        /// </summary>
        /// <param name="playerOutputPath">Player 输出路径。</param>
        /// <param name="playerOutputPath">Player output path.</param>
        /// <returns>Player Managed 目录绝对路径。</returns>
        /// <returns>Absolute Player Managed directory path.</returns>
        static private string ResolveManagedDirectoryForPlayer(string playerOutputPath)
        {
            var playerDirectory = Path.GetDirectoryName(playerOutputPath);
            if (string.IsNullOrWhiteSpace(playerDirectory))
            {
                throw new ArgumentException($"无法从 Player 输出路径解析目录: {playerOutputPath}", nameof(playerOutputPath));
            }

            var playerName = Path.GetFileNameWithoutExtension(playerOutputPath);
            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException($"无法从 Player 输出路径解析文件名: {playerOutputPath}", nameof(playerOutputPath));
            }

            return Path.Combine(playerDirectory, $"{playerName}_Data", "Managed");
        }

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
