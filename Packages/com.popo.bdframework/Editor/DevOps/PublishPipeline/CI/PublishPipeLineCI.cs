using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.PublishPipeline
{
    /// <summary>
    /// 构建相关的CI接口
    /// </summary>
    static public class PublishPipeLineCI
    {
        //CI相关接口
        // BDFramework.Editor.PublishPipeline.PublishPipeLineCI.BuildAssetBundle_iOS                   //打包iOS AssetBundle
        // BDFramework.Editor.PublishPipeline.PublishPipeLineCI.BuildAssetBundle_Android               //打包Android AssetBundle
        // BDFramework.Editor.PublishPipeline.PublishPipeLineCI.PublishPackage_AndroidDebug            //发布Android包体
        // BDFramework.Editor.PublishPipeline.PublishPipeLineCI.PublishPackage_AndroidRelease
        // BDFramework.Editor.PublishPipeline.PublishPipeLineCI.PublishPackage_iOSDebug                //发布iOS包体
        // BDFramework.Editor.PublishPipeline.PublishPipeLineCI.PublishPackage_iOSRelease
        
        static public string CI_ROOT_PATH    = "";
        static public string CI_PACKAGE_PATH = "";

        static PublishPipeLineCI()
        {
            //初始化编辑器
            BDFrameworkEditorBehaviour.InitBDFrameworkEditor();
            CI_ROOT_PATH    = IPath.Combine(BDApplication.ProjectRoot, "CI_TEMP");
            CI_PACKAGE_PATH = IPath.Combine(CI_ROOT_PATH, "Publish_Package");
            if (!Directory.Exists(CI_ROOT_PATH))
            {
                Directory.CreateDirectory(CI_ROOT_PATH);
            }
        }


        #region 构建资源

        /// <summary>
        /// 构建iOS
        /// </summary>
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
        
        #region 构建增量资源

        /// <summary>
        /// 构建增量iOS AssetBundle
        /// </summary>
        static public void BuildAdditionAssetBundle_iOS()
        {
        }

        /// <summary>
        /// 构建增量Android AssetBundle
        /// </summary>
        static public void BuildAdditionAssetBundle_Android()
        {
        }

        #endregion

        #region 代码打包检查

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
        static public void PublishPackage_AndroidDebug()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.Android, BuildPackageTools.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        static public void PublishPackage_AndroidRelease()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.Android, BuildPackageTools.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        static public void PublishPackage_iOSDebug()
        {
            CI_ROOT_PATH = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.IPhonePlayer, BuildPackageTools.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
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
                BuildPackageTools.BuildAPK(buildMode);
                //上传APK
                BDFrameworkServerTools.UploadAPK();
            }
            else if (platform == RuntimePlatform.IPhonePlayer)
            {
                //构建xcode
                BuildPackageTools.BuildIpa(buildMode);
            }

            //最后上传
            BDFrameworkServerTools.UploadFormFileServer(platform, localPath);
        }

        #endregion
    }
}