using System;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Environment;
using BDFramework.Editor.HotfixScript;
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


        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            if (Application.isBatchMode)
            {
                BDFrameworkEditorEnvironment.InitEditorEnvironment();
            }

        }

        /// <summary>
        /// 创建svn的处理器
        /// </summary>
        static private void CreateSVNProccesor()
        {
            //获取设置
            var devops_setting = BDEditorApplication.EditorSetting.DevOpsSetting;
            //资源仓库
            var store = devops_setting.AssetServiceVCSData;
              //svn仓库
            store = devops_setting.PackageServiceVCSData;
           }




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
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        [CI(Des = "发布母包Android-Release")]
        static public void PublishPackage_AndroidRelease()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        [CI(Des = "发布母包iOS-Debug")]
        static public void PublishPackage_iOSDebug()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "发布母包iOS-Release")]
        static public void PublishPackage_iOSRelease()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Release);
        }


        /// <summary>
        /// 构建包体
        /// </summary>
        static private void BuildPackage(BuildTarget buildTarget, BuildTools_ClientPackage.BuildMode buildMode)
        {

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
            // Debug.Log("【CI】 outdir:" + CI_PACKAGE_PATH);
            // var ret = BuildTools_ClientPackage.Build(buildMode, false, CI_PACKAGE_PATH, buildTarget);

        }

        #endregion


        
    }
}
