using System;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Environment;
using BDFramework.Editor.PublishPipeline;
using BDFramework.Editor.SVN;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 构建相关的CI接口
    /// </summary>
    static public class PublishPipeLineCI
    {
        static public string CI_ASSETS_PATH = "";
        static public string CI_PACKAGE_PATH = "";

        /// <summary>
        /// 资源svn仓库处理器
        /// </summary>
        static private SVNProcessor AssetsSvnProcessor { get; set; } = null;

        /// <summary>
        /// 包体svn仓库处理器
        /// </summary>
        static private SVNProcessor PackageSvnProcessor { get; set; } = null;

        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            if (Application.isBatchMode)
            {
                BDFrameworkEditorEnvironment.InitEditorEnvironment();
            }
            //
            CI_ASSETS_PATH = BApplication.DevOpsPublishAssetsPath; // IPath.Combine(BDApplication.DevOpsPath, "CI_TEMP");
            CI_PACKAGE_PATH = BApplication.DevOpsPublishPackagePath; // IPath.Combine(CI_ROOT_PATH, "CI_BUILD_PCK");
            if (!Directory.Exists(CI_ASSETS_PATH))
            {
                Directory.CreateDirectory(CI_ASSETS_PATH);
            }

            CreateSVNProccesor();
        }

        /// <summary>
        /// 创建svn的处理器
        /// </summary>
        static private void CreateSVNProccesor()
        {
            //获取设置
            var devops_setting = BDEditorApplication.BDFrameworkEditorSetting.DevOpsSetting;
            //资源仓库
            var store = devops_setting.AssetServiceVCSData;
            AssetsSvnProcessor = SVNProcessor.CreateSVNProccesor(store.Url, store.UserName, store.Psw, CI_ASSETS_PATH);
            //svn仓库
            store = devops_setting.PackageServiceVCSData;
            PackageSvnProcessor = SVNProcessor.CreateSVNProccesor(store.Url, store.UserName, store.Psw, CI_PACKAGE_PATH);
        }


        #region 构建资源

        /// <summary>
        /// 构建iOS
        /// </summary>
        [CI(Des = "构建资源iOS")]
        public static void BuildAssetBundle_iOS()
        {
            //更新
            SVNUpdate(AssetsSvnProcessor);

            //构建
            var ret = BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);

            //提交
            SVNCommit(BuildTarget.iOS, AssetsSvnProcessor);
        }

        /// <summary>
        /// 构建Android
        /// </summary>
        [CI(Des = "构建资源Android")]
        public static void BuildAssetBundle_Android()
        {
            // //更新
            SVNUpdate(AssetsSvnProcessor);
            //构建
            var ret = BuildAssetBundle(RuntimePlatform.Android, BuildTarget.Android);
            //提交
            SVNCommit(BuildTarget.Android, AssetsSvnProcessor);
        }

        /// <summary>
        /// 构建资源
        /// </summary>
        private static bool BuildAssetBundle(RuntimePlatform platform, BuildTarget target)
        {
            //1.搜集keyword
            ShaderCollection.CollectShaderVariant();
            //2.打包模式
            var ret = AssetBundleToolsV2.GenAssetBundle(platform, CI_ASSETS_PATH);

            return ret;
        }

        #endregion


        #region 代码打包检查

        [CI(Des = "代码检查")]
        public static void CheckEditorCode()
        {
            CheckCode();
        }

        /// <summary>
        /// 检测代码
        /// </summary>
        /// <returns></returns>
        public static bool CheckCode()
        {
            //检查下打包前的代码错
            var setting = new ScriptCompilationSettings();
            setting.options = ScriptCompilationOptions.Assertions;
            setting.target = BuildTarget.Android;
            var ret = PlayerBuildInterface.CompilePlayerScripts(setting, BApplication.Library + "/BuildTest");
            if (ret.assemblies.Contains("Assembly-CSharp.dll"))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
            //检查打包脚本
            EditorWindow_ScriptBuildDll.RoslynBuild(CI_ASSETS_PATH, RuntimePlatform.Android, ScriptBuildTools.BuildMode.Release);
        }

        #endregion

        #region 发布母包

        /// <summary>
        /// 发布包体 AndroidDebug
        /// </summary>
        [CI(Des = "发布母包Android-Debug")]
        static public void PublishPackage_AndroidDebug()
        {
            //更新
            BuildPackage(BuildTarget.Android, BuildPackageTools.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        [CI(Des = "发布母包Android-Release")]
        static public void PublishPackage_AndroidRelease()
        {
            BuildPackage(BuildTarget.Android, BuildPackageTools.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        [CI(Des = "发布母包iOS-Debug")]
        static public void PublishPackage_iOSDebug()
        {
            BuildPackage(BuildTarget.iOS, BuildPackageTools.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "发布母包iOS-Release")]
        static public void PublishPackage_iOSRelease()
        {
            BuildPackage(BuildTarget.iOS, BuildPackageTools.BuildMode.Release);
        }


        /// <summary>
        /// 构建包体
        /// </summary>
        static private void BuildPackage(BuildTarget buildTarget, BuildPackageTools.BuildMode buildMode)
        {
            //- 默认下载svn管理的仓库,用来打包
            SVNUpdate(AssetsSvnProcessor);
            //- 更新包体仓库
            SVNUpdate(PackageSvnProcessor);
            // var localPath = string.Format("{0}/{1}/Art", CI_ASSETS_PATH, BDApplication.GetPlatformPath(platform));
            // //1.下载资源已有、Sql
            // //2.打包dll
            // ScriptBuildTools.BuildMode mode = buildMode == BuildPackageTools.BuildMode.Debug ? ScriptBuildTools.BuildMode.Debug : ScriptBuildTools.BuildMode.Release;
            // EditorWindow_ScriptBuildDll.RoslynBuild(CI_ASSETS_PATH, platform, mode);
            // //3.构建空包即可
            //构建资源
            // if (platform == RuntimePlatform.Android)
            // {
            //     BuildAssetBundle(RuntimePlatform.Android, BuildTarget.Android);
            // }
            // else if (platform == RuntimePlatform.IPhonePlayer)
            // {
            //     BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
            // }
            //加载配置
            // BuildPackageTools.LoadConfig(buildMode);
            //
            bool ret = false;
            if (buildTarget == BuildTarget.Android)
            {
                Debug.Log("【CI】 outdir:" + CI_PACKAGE_PATH);
                ret = BuildPackageTools.BuildAPK(buildMode, false, CI_PACKAGE_PATH);
            }
            else if (buildTarget == BuildTarget.iOS)
            {
                //构建xcode、ipa
                Debug.Log("【CI】 outdir:" + CI_PACKAGE_PATH);
                ret = BuildPackageTools.BuildIpa(buildMode, false, CI_PACKAGE_PATH);
            }

            if (ret)
            {
                Debug.Log("【CI】Build package success，begin commit!");
                SVNCommit(buildTarget, PackageSvnProcessor);
            }
            else
            {
                Debug.Log("【CI】Build package fail，dont commit!");
            }
        }

        #endregion

        #region SVN操作

        /// <summary>
        /// SVN更新
        /// </summary>
        static private void SVNUpdate(SVNProcessor svnProcessor)
        {
            //存在仓库
            if (svnProcessor.IsExsitSvnStore())
            {
                svnProcessor.CleanUp();
                svnProcessor.RevertForce();
                svnProcessor.Update();
            }
            else
            {
                svnProcessor.CheckOut();
            }
        }

        /// <summary>
        /// SVN提交
        /// </summary>
        static private void SVNCommit(BuildTarget buildTarget, SVNProcessor svnProcessor)
        {
            //存在仓库
            var svntag = svnProcessor.LocalSVNRootPath + "/.svn";
            if (Directory.Exists(svntag))
            {
                //1.获取被删除文件提交
                var delFiles = svnProcessor.GetStatus(SVNProcessor.Status.Deleted);
                svnProcessor.ForceDelete(delFiles);
                //2.添加修改的文件
                var motifyFiles = svnProcessor.GetStatus(SVNProcessor.Status.Motify);
                svnProcessor.ForceAdd(motifyFiles);
                //3.添加新文件
                var addFiles = svnProcessor.GetStatus(SVNProcessor.Status.NewFile);
                svnProcessor.ForceAdd(addFiles);
                //提交
                svnProcessor.Commit(log: $"{buildTarget.ToString()} - AssetBundle Commit !");
            }
        }

        #endregion

        #region Git操作

        #endregion

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "Test")]
        static public void Test()
        {
            Debug.Log("Test CI passed!");
            SVNCommit(BuildTarget.NoTarget, PackageSvnProcessor);
            //var b = BDEditorApplication.IsPlatformModuleInstalled(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }
}
