using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BDFramework.Editor.Asset;
using BDFramework.Editor.EditorLife;
using Code.BDFramework.Editor;
using LitJson;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BDFramework.Editor
{
    static public class BuildPipeLine_CI
    {
        private static string outputPath = "";

        static BuildPipeLine_CI()
        {
            //初始化编辑器
            BDFrameEditorLife.InitBDEditorLife();
            //
            outputPath = Application.streamingAssetsPath;
        }

        /// <summary>
        /// 获取平台
        /// </summary>
        /// <param name="platform"></param>
        public static string GetPlatform(RuntimePlatform platform)
        {
            if (platform == RuntimePlatform.IPhonePlayer)
            {
                return "iOS";
            }
            else
            {
                return "Android";
            }
        }

        #region 构建资源

        /// <summary>
        /// 构建iOS
        /// </summary>
        public static void BuildAssetBundle_iOS(string version = "")
        {
            BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
        }

        /// <summary>
        /// 构建Android
        /// </summary>
        public static void BuildAssetBundle_Android(string version = "")
        {
            BuildAssetBundle(RuntimePlatform.Android, BuildTarget.Android);
        }

        /// <summary>
        /// 构建资源
        /// </summary>
        private static void BuildAssetBundle(RuntimePlatform platform, BuildTarget target)
        {
            //1.搜集keyword
            ShaderCollection.GenShaderVariant();
            //2.打包模式
            var config = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig;
            AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform, target, BuildAssetBundleOptions.ChunkBasedCompression, true, config.AESCode);
            //
            UploadFormFileServer(platform);
        }

        #endregion

        #region 代码打包检查

        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL(RuntimePlatform platform)
        {
            EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, platform, ScriptBuildTools.BuildMode.Release);
        }

        #endregion

        #region 发布资源版本

        #endregion
        
        #region 第一次构建包体

        static public void BuildAndroidDebug()
        {
            BuildPackage(RuntimePlatform.Android, EditorBuildPackage.BuildMode.Debug);
        }

        static public void BuildAndroidRelease()
        {
            BuildPackage(RuntimePlatform.Android, EditorBuildPackage.BuildMode.Release);
        }

        static public void BuildIOSDebug()
        {
            BuildPackage(RuntimePlatform.IPhonePlayer, EditorBuildPackage.BuildMode.Debug);
        }

        static public void BuildIOSRelease()
        {
            BuildPackage(RuntimePlatform.IPhonePlayer, EditorBuildPackage.BuildMode.Release);
        }


        /// <summary>
        /// 构建包体
        /// </summary>
        static private void BuildPackage(RuntimePlatform platform, EditorBuildPackage.BuildMode buildMode)
        {
            //1.下载资源已有、Sql
            var ret = DownloadFormFileServer(platform);
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
                EditorBuildPackage.BuildAPK();
            }
            else if (platform == RuntimePlatform.IPhonePlayer)
            {
                EditorBuildPackage.BuildIpa();
            }
        }

        #endregion
        
        #region 增量构建包体

        #endregion

        #region 上传下载美术资源

        public enum FileProtocol
        {
            GetAllFile,
            GetFile,
            Upload,
        }


        /// <summary>
        /// 从服务器下载资源
        /// </summary>
        /// <param name="platform"></param>
        private static bool DownloadFormFileServer(RuntimePlatform platform)
        {
            var platformStr = GetPlatform(platform);
            var url         = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl;
            var protocol    = string.Format("{0}/{1}/{2}", url, nameof(FileProtocol.GetAllFile), platformStr);
            var webclient   = new WebClient();
            //所有文件的配置
            string fileConfig = "";
            try
            {
                fileConfig = webclient.DownloadString(protocol);
            }
            catch (Exception e)
            {
                Debug.LogError("服务器文件获取失败、或者无服务");
                return true;
            }

            //删除本地所有素材
            var localPath = string.Format("{0}/{1}/Art", outputPath, platformStr);
            Directory.Delete(localPath, true);
            //
            var fileQueue = JsonMapper.ToObject<Queue<string>>(fileConfig);
            int ErrorCont = 0;
            while (fileQueue.Count > 0)
            {
                var filename = fileQueue.Dequeue();
                var furl     = string.Format("{0}/{1}/{2}/{3}", url, nameof(FileProtocol.GetFile), platformStr, filename);
                var savePath = string.Format("{0}/{1}", localPath, filename);

                try
                {
                    webclient.DownloadFile(furl, savePath);
                }
                catch (Exception e)
                {
                    //重新压栈执行
                    fileQueue.Enqueue(filename);
                    ErrorCont++;
                    if (ErrorCont > 1000)
                    {
                        Debug.LogError("失败次数过多，本次任务失败");
                        return false;
                    }
                }
            }

            webclient.Dispose();
            return true;
        }

        /// <summary>
        /// 上传服务器
        /// </summary>
        /// <param name="platform"></param>
        private static void UploadFormFileServer(RuntimePlatform platform)
        {
            var platformStr = GetPlatform(platform);
            var url         = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl;
            var protocol    = string.Format("{0}/{1}/{2}", url, nameof(FileProtocol.Upload), platformStr);
            var webclient   = new WebClient();
            //获取本地所有素材
            var localPath = string.Format("{0}/{1}/Art", outputPath, platformStr);
            var fs        = Directory.GetFiles(localPath, "*.*", SearchOption.AllDirectories).Where((f) => !f.EndsWith(".meta"));
            var fsQueue   = new Queue<string>(fs);
            //
            int ErrorCont = 0;
            foreach (var f in fsQueue)
            {
                try
                {
                    webclient.UploadFile(protocol, f);
                }
                catch (Exception e)
                {
                    fsQueue.Enqueue(f);
                    //重新压栈执行
                    ErrorCont++;
                    if (ErrorCont > 10)
                    {
                        Debug.LogError("失败次数过多，本次任务失败");
                        return;
                    }
                }
            }

            webclient.Dispose();
        }

        #endregion
    }
}