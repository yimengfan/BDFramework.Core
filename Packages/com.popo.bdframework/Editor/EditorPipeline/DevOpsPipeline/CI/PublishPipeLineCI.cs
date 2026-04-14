using System;
using System.Collections.Generic;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Environment;
using BDFramework.Editor.HotfixScript;
using BDFramework.Editor.Table;
using BDFramework.ResourceMgr;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// BDFramework Editor 侧的 BatchMode 发布入口封装。
    /// 该类只保留 TeamCity 或命令行通过 <c>-executeMethod</c> 调用的稳定入口，不再承载具体构建、参数解析或 Android 环境探测实现。
    /// 具体实现分别下沉到母包、ClientRes、表格和文件服务器验证各自的 owner 模块中，避免 CI 包装层继续膨胀。
    /// </summary>
    /// <example>
    /// Unity -batchmode -projectPath &lt;project&gt; -executeMethod BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid
    /// </example>
    static public class PublishPipeLineCI
    {
        const string BuildDebugBatchArgName = "-buildDebug";
        const string EnableE2ETestSymbol = "ENABLE_E2ETEST";

        /// <summary>
        /// Talos E2E 调试宏作用域。
        /// 只在当前目标平台临时补齐脚本宏，结束后恢复到进入前的状态，避免污染后续非 E2E 构建。
        /// </summary>
        sealed class TalosDebugDefineScope : IDisposable
        {
            readonly BuildTargetGroup buildTargetGroup;
            readonly bool addedDebugSymbol;
            readonly bool addedEnableE2ETestSymbol;

            /// <summary>
            /// 为指定平台创建 Talos 调试宏作用域。
            /// </summary>
            public TalosDebugDefineScope(BuildTarget buildTarget, bool includeDebugSymbol)
            {
                this.buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
                if (this.buildTargetGroup == BuildTargetGroup.Unknown)
                {
                    throw new Exception($"未知的构建目标组: {buildTarget}");
                }

                this.addedDebugSymbol = includeDebugSymbol && TryAddSymbol("DEBUG");
                this.addedEnableE2ETestSymbol = TryAddSymbol(EnableE2ETestSymbol);
                Debug.Log(
                    $"【CI】Talos 调试宏作用域已启用 Target:{buildTarget} IncludeDebug:{includeDebugSymbol} AddedDebug:{this.addedDebugSymbol} AddedE2E:{this.addedEnableE2ETestSymbol}");
            }

            /// <summary>
            /// 仅在当前目标平台缺失指定宏时追加，避免重复拼接造成宏串污染。
            /// </summary>
            bool TryAddSymbol(string symbol)
            {
                var symbols = new List<string>(
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(this.buildTargetGroup)
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                if (symbols.Contains(symbol))
                {
                    return false;
                }

                symbols.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(this.buildTargetGroup, string.Join(";", symbols));
                return true;
            }

            /// <summary>
            /// 作用域结束时仅移除本次临时追加的宏，不影响进入作用域前已有配置。
            /// </summary>
            void RemoveSymbol(string symbol)
            {
                var symbols = new List<string>(
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(this.buildTargetGroup)
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                if (!symbols.Remove(symbol))
                {
                    return;
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(this.buildTargetGroup, string.Join(";", symbols));
            }

            /// <summary>
            /// 释放 Talos 调试宏作用域，并恢复当前平台在进入作用域前的宏状态。
            /// </summary>
            public void Dispose()
            {
                if (this.addedDebugSymbol)
                {
                    RemoveSymbol("DEBUG");
                }

                if (this.addedEnableE2ETestSymbol)
                {
                    RemoveSymbol(EnableE2ETestSymbol);
                }
            }
        }

        // 在 BatchMode 入口第一次触达时补齐编辑器环境，确保后续 owner 可以直接复用既有管线。
        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            if (Application.isBatchMode)
            {
                BDFrameworkEditorEnvironment.InitEditorEnvironment();
            }

        }




        /// <summary>
        /// 判断当前 BatchMode 调用是否显式请求 Debug 构建。
        /// 统一复用 <c>-buildDebug</c> 参数，避免 TeamCity、Python wrapper 和 owner 层各自维护一套布尔解析逻辑。
        /// </summary>
        static public bool IsDebugBuildRequested()
        {
            return IsDebugBuildRequested(System.Environment.GetCommandLineArgs());
        }

        /// <summary>
        /// 判断给定参数列表中是否显式请求 Debug 构建。
        /// 该重载主要供测试和局部复用使用。
        /// </summary>
        static public bool IsDebugBuildRequested(IReadOnlyList<string> args)
        {
            return BatchModeCommandLine.GetBoolArg(args, BuildDebugBatchArgName, false);
        }

        /// <summary>
        /// 根据 BatchMode 参数解析母包构建模式。
        /// 未显式开启 Debug 时统一回退到 Release，保持现有 CI 任务兼容。
        /// </summary>
        static public BuildTools_ClientPackage.BuildMode ResolveClientPackageBuildModeForBatchMode()
        {
            return ResolveClientPackageBuildModeForBatchMode(System.Environment.GetCommandLineArgs());
        }

        /// <summary>
        /// 根据显式参数列表解析母包构建模式。
        /// 该重载主要供测试验证参数路由使用。
        /// </summary>
        static public BuildTools_ClientPackage.BuildMode ResolveClientPackageBuildModeForBatchMode(IReadOnlyList<string> args)
        {
            return IsDebugBuildRequested(args)
                ? BuildTools_ClientPackage.BuildMode.Debug
                : BuildTools_ClientPackage.BuildMode.Release;
        }

        /// <summary>
        /// 执行指定平台的母包 BatchMode 构建。
        /// 当显式开启 Debug 时，沿用现有母包 Debug 模式，并额外补齐 Talos E2E 编译宏。
        /// </summary>
        static private void BuildClientPackageForBatchMode(BuildTarget buildTarget)
        {
            var buildMode = ResolveClientPackageBuildModeForBatchMode();
            var enableTalosDebug = buildMode == BuildTools_ClientPackage.BuildMode.Debug;
            Debug.Log($"【CI】BuildClientPackage Target:{buildTarget} BuildMode:{buildMode} TalosDebug:{enableTalosDebug}");

            if (enableTalosDebug)
            {
                using (new TalosDebugDefineScope(buildTarget, includeDebugSymbol: false))
                {
                    BuildTools_ClientPackage.BuildClientPackageForBatchMode(buildTarget, buildMode);
                }

                return;
            }

            BuildTools_ClientPackage.BuildClientPackageForBatchMode(buildTarget, buildMode);
        }

        /// <summary>
        /// 执行指定平台的热更代码 BatchMode 构建。
        /// Debug 模式下会临时注入 DEBUG 与 ENABLE_E2ETEST，确保 Talos E2E 注册代码参与编译。
        /// </summary>
        static private void BuildClientResHotfixCodeForBatchMode(BuildTarget buildTarget)
        {
            var enableDebugBuild = IsDebugBuildRequested();
            Debug.Log($"【CI】BuildClientResHotfixCode Target:{buildTarget} TalosDebug:{enableDebugBuild}");

            if (enableDebugBuild)
            {
                using (new TalosDebugDefineScope(buildTarget, includeDebugSymbol: true))
                {
                    BuildTools_Assets.BuildClientResForBatchMode(buildTarget,
                        BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
                }

                return;
            }

            BuildTools_Assets.BuildClientResForBatchMode(buildTarget,
                BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
        }

        #region 代码打包检查

        [CI(Des = "代码检查")]
        public static void CheckEditorCode()
        {
            BuildTools_HotfixScript.CheckEditorCode();
        }


        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
          }

        #endregion

        #region 发布母包

        /// <summary>
        /// 发布包体 AndroidDebug
        /// </summary>
        [CI(Des = "发布母包Android-Debug")]
        static public void PublishPackage_AndroidDebug()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.Android,
                BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        [CI(Des = "发布母包Android-Release")]
        static public void PublishPackage_AndroidRelease()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.Android,
                BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        [CI(Des = "发布母包iOS-Debug")]
        static public void PublishPackage_iOSDebug()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.iOS,
                BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "发布母包iOS-Release")]
        static public void PublishPackage_iOSRelease()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.iOS,
                BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 WindowsDebug
        /// </summary>
        [CI(Des = "发布母包Windows-Debug")]
        static public void PublishPackage_WindowsDebug()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.StandaloneWindows64,
                BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 WindowsRelease
        /// </summary>
        [CI(Des = "发布母包Windows-Release")]
        static public void PublishPackage_WindowsRelease()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.StandaloneWindows64,
                BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 Android Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Android-Release")]
        static public void BuildClientPackageAndroid()
        {
            BuildClientPackageForBatchMode(BuildTarget.Android);
        }

        /// <summary>
        /// BatchMode: 构建 iOS Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包iOS-Release")]
        static public void BuildClientPackageIOS()
        {
            BuildClientPackageForBatchMode(BuildTarget.iOS);
        }

        /// <summary>
        /// BatchMode: 构建 Windows Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Windows-Release")]
        static public void BuildClientPackageWindows()
        {
            BuildClientPackageForBatchMode(BuildTarget.StandaloneWindows64);
        }

        /// <summary>
        /// 执行指定平台的文件服务器 BatchMode 验证。
        /// 该协调器只负责收敛命令行参数、补齐 Android External Tools、调用显式运行时验证入口，并把结果写成稳定日志与异常。
        /// </summary>
        static private void VerifyClientRes(BuildTarget buildTarget)
        {
            var platform = BApplication.GetRuntimePlatform(buildTarget);
            if (buildTarget == BuildTarget.Android)
            {
                AndroidExternalToolsBatchResolver.EnsureAndroidExternalToolsForBatchMode();
            }

            AssetsVersionController.VerifyFileServerAssetsForBatchModeFromCommandLine(platform);
        }

        /// <summary>
        /// BatchMode: 构建 Android 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码Android")]
        static public void BuildCodeAndroid()
        {
            BuildClientResHotfixCodeForBatchMode(BuildTarget.Android);
        }

        /// <summary>
        /// BatchMode: 构建 iOS 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码iOS")]
        static public void BuildCodeIOS()
        {
            BuildClientResHotfixCodeForBatchMode(BuildTarget.iOS);
        }

        /// <summary>
        /// BatchMode: 构建 Windows 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码Windows")]
        static public void BuildCodeWindows()
        {
            BuildClientResHotfixCodeForBatchMode(BuildTarget.StandaloneWindows64);
        }

        /// <summary>
        /// BatchMode: 构建 Android 热更 AssetBundle。
        /// </summary>
        [CI(Des = "BatchMode构建热更Assetbundle Android")]
        static public void BuildAssetbundleAndroid()
        {
            BuildTools_Assets.BuildClientResForBatchMode(BuildTarget.Android,
                BuildTools_Assets.BuildPackageOption.BuildArtAssets);
        }

        /// <summary>
        /// BatchMode: 构建 iOS 热更 AssetBundle。
        /// </summary>
        [CI(Des = "BatchMode构建热更Assetbundle iOS")]
        static public void BuildAssetbundleIOS()
        {
            BuildTools_Assets.BuildClientResForBatchMode(BuildTarget.iOS,
                BuildTools_Assets.BuildPackageOption.BuildArtAssets);
        }

        /// <summary>
        /// BatchMode: 构建 Windows 热更 AssetBundle。
        /// </summary>
        [CI(Des = "BatchMode构建热更Assetbundle Windows")]
        static public void BuildAssetbundleWindows()
        {
            BuildTools_Assets.BuildClientResForBatchMode(BuildTarget.StandaloneWindows64,
                BuildTools_Assets.BuildPackageOption.BuildArtAssets);
        }

        /// <summary>
        /// BatchMode: 验证 Android 文件服务器热更资源链路。
        /// 该入口会读取 TeamCity 透传的文件服务器地址与三段期望版本号，并在 Unity 内执行真实下载与可用性校验。
        /// </summary>
        [CI(Des = "BatchMode验证热更资源 Android")]
        static public void VerifyClientResAndroid()
        {
            VerifyClientRes(BuildTarget.Android);
        }

        /// <summary>
        /// BatchMode: 验证 iOS 文件服务器热更资源链路。
        /// </summary>
        [CI(Des = "BatchMode验证热更资源 iOS")]
        static public void VerifyClientResIOS()
        {
            VerifyClientRes(BuildTarget.iOS);
        }

        /// <summary>
        /// BatchMode: 验证 Windows 文件服务器热更资源链路。
        /// </summary>
        [CI(Des = "BatchMode验证热更资源 Windows")]
        static public void VerifyClientResWindows()
        {
            VerifyClientRes(BuildTarget.StandaloneWindows64);
        }

        /// <summary>
        /// BatchMode: 构建统一表格数据库。
        /// 产物会写到 CI 输出根目录，供后续上传脚本发布为共享 Table 制品。
        /// </summary>
        [CI(Des = "BatchMode构建统一表格")]
        static public void BuildTable()
        {
            BuildTools_Excel2SQLite.BuildTableForBatchMode();
        }

        #endregion


        
    }
}
