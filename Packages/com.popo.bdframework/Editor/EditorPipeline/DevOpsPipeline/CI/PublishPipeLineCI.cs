using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.PublishPipeline;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 构建相关的CI接口
    /// </summary>
    static public class PublishPipeLineCI
    {
        static public string CI_ROOT_PATH    = "";
        static public string CI_PACKAGE_PATH = "";

        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            BDFrameworkEditorBehaviour.InitBDFrameworkEditor();
            //
            CI_ROOT_PATH    = IPath.Combine(BDApplication.DevOpsPath, "CI_TEMP");
            CI_PACKAGE_PATH = IPath.Combine(CI_ROOT_PATH, "CI_BUILD_PCK");
            if (!Directory.Exists(CI_ROOT_PATH))
            {
                Directory.CreateDirectory(CI_ROOT_PATH);
            }
        }


        #region 构建资源

        /// <summary>
        /// 构建iOS
        /// </summary>
        [CI(Des = "构建资源iOS")]
        public static void BuildAssetBundle_iOS()
        {
            var localPath = string.Format("{0}/{1}/Art", CI_ROOT_PATH, BDApplication.GetPlatformPath(RuntimePlatform.IPhonePlayer));
            //下载
            BDFrameworkServerTools.DownloadAssetBundle(RuntimePlatform.IPhonePlayer, localPath);
            //构建
            var ret = BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
            if (ret)
            {
                //执行打包后上传
                BDFrameworkServerTools.UploadFormFileServer(RuntimePlatform.IPhonePlayer, localPath);
            }
            else
            {
                Debug.LogError("未有资源变动，无需上传!");
            }
        }

        /// <summary>
        /// 构建Android
        /// </summary>
        [CI(Des = "构建资源Android")]
        public static void BuildAssetBundle_Android()
        {
            var localPath = string.Format("{0}/{1}/Art", CI_ROOT_PATH, BDApplication.GetPlatformPath(RuntimePlatform.Android));
            //下载
            BDFrameworkServerTools.DownloadAssetBundle(RuntimePlatform.Android, localPath);
            //构建
            var ret = BuildAssetBundle(RuntimePlatform.Android, BuildTarget.Android);
            if (ret)
            {
                //执行打包后上传
                BDFrameworkServerTools.UploadFormFileServer(RuntimePlatform.Android, localPath);
            }
            else
            {
                Debug.LogError("未有资源变动，无需上传!");
            }
        }

        /// <summary>
        /// 构建资源
        /// </summary>
        private static bool BuildAssetBundle(RuntimePlatform platform, BuildTarget target)
        {
            //1.搜集keyword
            ShaderCollection.SimpleGenShaderVariant();
            //2.打包模式
            var config = BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildAssetBundle;
            return AssetBundleEditorToolsV2.GenAssetBundle(CI_ROOT_PATH, platform);
        }

        #endregion
        


        #region 代码打包检查

        [CI(Des = "代码检查")]
        public static void CheckEditorCode()
        {
            //检查下打包前的代码错
            UnityEditor.BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.Android);
        }
        
        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
            
            //检查打包脚本
            EditorWindow_ScriptBuildDll.RoslynBuild(CI_ROOT_PATH, RuntimePlatform.Android, ScriptBuildTools.BuildMode.Release);
        }

        #endregion

        #region 发布母包

        /// <summary>
        /// 发布包体 AndroidDebug
        /// </summary>
        [CI(Des = "发布母包Android-Debug")]
        static public void PublishPackage_AndroidDebug()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.Android, BuildPackageTools.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        [CI(Des = "发布母包Android-Release")]
        static public void PublishPackage_AndroidRelease()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.Android, BuildPackageTools.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        [CI(Des = "发布母包iOS-Debug")]
        static public void PublishPackage_iOSDebug()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.IPhonePlayer, BuildPackageTools.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "发布母包iOS-Release")]
        static public void PublishPackage_iOSRelease()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.IPhonePlayer, BuildPackageTools.BuildMode.Release);
        }


        /// <summary>
        /// 构建包体
        /// </summary>
        static private void BuildPackage(RuntimePlatform platform, BuildPackageTools.BuildMode buildMode)
        {
            var localPath = string.Format("{0}/{1}/Art", CI_ROOT_PATH, BDApplication.GetPlatformPath(platform));
            //1.下载资源已有、Sql
            var ret = BDFrameworkServerTools.DownloadAssetBundle(platform, localPath);
            //2.打包dll
            ScriptBuildTools.BuildMode mode = buildMode == BuildPackageTools.BuildMode.Debug ? ScriptBuildTools.BuildMode.Debug : ScriptBuildTools.BuildMode.Release;
            EditorWindow_ScriptBuildDll.RoslynBuild(CI_ROOT_PATH, platform, mode);
            //3.构建空包即可
            if (!ret)
            {
                //构建资源
                if (platform == RuntimePlatform.Android)
                {
                    BuildAssetBundle(RuntimePlatform.Android, BuildTarget.Android);
                }
                else if (platform == RuntimePlatform.IPhonePlayer)
                {
                    BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
                }
            }

            //加载配置
            BuildPackageTools.LoadConfig(buildMode);
            //
            if (platform == RuntimePlatform.Android)
            {
                BuildPackageTools.BuildAPK(buildMode,BDApplication.DevOpsPublishPackagePath);
                //上传APK
                BDFrameworkServerTools.UploadAPK();
            }
            else if (platform == RuntimePlatform.IPhonePlayer)
            {
                //构建xcode
                BuildPackageTools.BuildIpa(buildMode,BDApplication.DevOpsPublishPackagePath);
            }

            //最后上传
            BDFrameworkServerTools.UploadFormFileServer(platform, localPath);
        }

        #endregion
    }
}