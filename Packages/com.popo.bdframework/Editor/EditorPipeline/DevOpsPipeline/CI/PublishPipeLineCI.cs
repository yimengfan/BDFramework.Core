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
        // 在 BatchMode 入口第一次触达时补齐编辑器环境，确保后续 owner 可以直接复用既有管线。
        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            if (Application.isBatchMode)
            {
                BDFrameworkEditorEnvironment.InitEditorEnvironment();
            }

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
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.Android,
                BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 iOS Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包iOS-Release")]
        static public void BuildClientPackageIOS()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.iOS,
                BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 Windows Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Windows-Release")]
        static public void BuildClientPackageWindows()
        {
            BuildTools_ClientPackage.BuildClientPackageForBatchMode(BuildTarget.StandaloneWindows64,
                BuildTools_ClientPackage.BuildMode.Release);
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
            BuildTools_Assets.BuildClientResForBatchMode(BuildTarget.Android,
                BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
        }

        /// <summary>
        /// BatchMode: 构建 iOS 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码iOS")]
        static public void BuildCodeIOS()
        {
            BuildTools_Assets.BuildClientResForBatchMode(BuildTarget.iOS,
                BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
        }

        /// <summary>
        /// BatchMode: 构建 Windows 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码Windows")]
        static public void BuildCodeWindows()
        {
            BuildTools_Assets.BuildClientResForBatchMode(BuildTarget.StandaloneWindows64,
                BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
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
