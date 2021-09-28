using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BDFramework.Editor.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 构建相关的CI接口
    /// </summary>
    static public class BuildPipeLine_CI
    {
        private static string outputPath = "";

        static BuildPipeLine_CI()
        {
            //初始化编辑器
            BDFrameEditorLife.InitBDFrameworkEditor();
            //
            outputPath = BDApplication.ProjectRoot + "/CI_TEMP";
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
        }


        #region 构建资源

        /// <summary>
        /// 构建iOS
        /// </summary>
        public static void BuildAssetBundle_iOS()
        {
            var localPath = string.Format("{0}/{1}/Art", outputPath, BDApplication.GetPlatformPath(RuntimePlatform.IPhonePlayer));
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
            //删除目录防止下次导入

            Directory.Delete(localPath, true);
        }

        /// <summary>
        /// 构建Android
        /// </summary>
        public static void BuildAssetBundle_Android()
        {
            var localPath = string.Format("{0}/{1}/Art", outputPath, BDApplication.GetPlatformPath(RuntimePlatform.Android));
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
            
            //删除目录防止下次导入
            Directory.Delete(localPath, true);
        }

        /// <summary>
        /// 构建资源
        /// </summary>
        private static bool BuildAssetBundle(RuntimePlatform platform, BuildTarget target)
        {
            //1.搜集keyword
            ShaderCollection.SimpleGenShaderVariant();
            //2.打包模式
            var config = BDEditorApplication.BdFrameEditorSetting.BuildAssetBundle;
            return AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform, target, BuildAssetBundleOptions.ChunkBasedCompression, true, config.AESCode);
        }

        #endregion

        #region 代码打包检查

        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
            //检查打包脚本
            EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, RuntimePlatform.Android, ScriptBuildTools.BuildMode.Release);
            //检查下打包前的代码错
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.Android);
        }

        #endregion



        #region 第一次构建包体

        static public void BuildAndroidDebug()
        {
            outputPath = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.Android, EditorBuildPackage.BuildMode.Debug);
        }

        static public void BuildAndroidRelease()
        {
            outputPath = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.Android, EditorBuildPackage.BuildMode.Release);
        }

        static public void BuildIOSDebug()
        {
            outputPath = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.IPhonePlayer, EditorBuildPackage.BuildMode.Debug);
        }

        static public void BuildIOSRelease()
        {
            outputPath = Application.streamingAssetsPath;
            BuildPackage(RuntimePlatform.IPhonePlayer, EditorBuildPackage.BuildMode.Release);
        }


        /// <summary>
        /// 构建包体
        /// </summary>
        static private void BuildPackage(RuntimePlatform platform, EditorBuildPackage.BuildMode buildMode)
        {
            var localPath = string.Format("{0}/{1}/Art", outputPath, BDApplication.GetPlatformPath(platform));
            //1.下载资源已有、Sql
            var ret = BDFrameworkServerTools.DownloadAssetBundle(platform,localPath);
            //2.打包dll
            ScriptBuildTools.BuildMode mode = buildMode == EditorBuildPackage.BuildMode.Debug ? ScriptBuildTools.BuildMode.Debug : ScriptBuildTools.BuildMode.Release;
            EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, platform, mode);
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
            EditorBuildPackage.LoadConfig(buildMode);
            //
            if (platform == RuntimePlatform.Android)
            {
                EditorBuildPackage.BuildAPK(buildMode);
                //上传APK
                BDFrameworkServerTools.UploadAPK();
            }
            else if (platform == RuntimePlatform.IPhonePlayer)
            {
                //构建xcode
                EditorBuildPackage.BuildIpa(buildMode);
            }

            //最后上传
            BDFrameworkServerTools.UploadFormFileServer(platform,localPath);
        }

        #endregion

        #region 增量构建包体

        #endregion
    }
}