using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using BDFramework.Editor.Asset;
using BDFramework.Editor.EditorLife;
using Code.BDFramework.Core.Tools;
using Code.BDFramework.Editor;
using LitJson;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BDFramework.Editor
{
    public class Response
    {
        /// <summary>
        /// 返回码
        /// 0=失败
        /// 1=成功
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 返回的消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 返回的内容
        /// </summary>
        public JsonData Content { get; set; }

        public bool IsObject { get; set; } = false;

        //隐藏构造函数
        private Response()
        {
        }

        /// <summary>
        /// 创建response
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        static public Response Cretate(string json)
        {
            var ret = new Response();
            var jw  = JsonMapper.ToObject(json);

            //code
            int code = -1;
            int.TryParse(jw["code"].ToString(), out code);
            ret.Code = code;
            //msg
            var msg = jw["msg"];
            if (msg != null)
            {
                ret.Msg = msg.ToJson();
            }

            //content
            ret.Content = jw["content"];
            return ret;
        }

        /// <summary>
        /// 获取返回内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetContent<T>()
        {
            if (this.Content.IsObject || this.Content.IsArray)
            {
                return JsonMapper.ToObject<T>(this.Content.ToJson());
            }
            else
            {
                var type      = typeof(T);
                var targetObj = Convert.ChangeType(Content.ToString(), type);

                return (T) targetObj;
            }

            return (T)new object();
        }
    }

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


        #region 构建资源

        /// <summary>
        /// 构建iOS
        /// </summary>
        public static void BuildAssetBundle_iOS()
        {
            //下载
            DownloadFormFileServer(RuntimePlatform.IPhonePlayer);
            //构建
           var ret =BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
            //有资源变更，则上传
            if (ret)
            {
                UploadFormFileServer(RuntimePlatform.IPhonePlayer);
            }
        }

        /// <summary>
        /// 构建Android
        /// </summary>
        public static void BuildAssetBundle_Android()
        {
            //下载
            DownloadFormFileServer(RuntimePlatform.Android);
            //构建
            var ret = BuildAssetBundle(RuntimePlatform.Android, BuildTarget.Android);
            //有资源变更，则上传
            if (ret)
            {
                UploadFormFileServer(RuntimePlatform.Android);
            }
        }

        /// <summary>
        /// 构建资源
        /// </summary>
        private static bool BuildAssetBundle(RuntimePlatform platform, BuildTarget target)
        {
            //1.搜集keyword
            ShaderCollection.GenShaderVariant();
            //2.打包模式
            var config = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig;
            var ret =  AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform, target, BuildAssetBundleOptions.ChunkBasedCompression, true, config.AESCode);
            return ret;
        }

        #endregion

        #region 代码打包检查

        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
            EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, RuntimePlatform.Android, ScriptBuildTools.BuildMode.Release);
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

            //最后上传
            UploadFormFileServer(platform);
        }

        #endregion

        #region 增量构建包体

        #endregion

        #region 上传下载美术资源

        public enum ABServer_Protocol
        {
            GetLastUploadFiles,
            Upload,
            Download,
            GetLastVersion
        }


        /// <summary>
        /// 从服务器下载资源
        /// </summary>
        /// <param name="platform"></param>
        private static bool DownloadFormFileServer(RuntimePlatform platform)
        {
            var platformStr = BDApplication.GetPlatformPath(platform);
            var url         = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl + "/Assetbundle";
            var webclient   = new WebClient();

            //获取最新版本的文件 //url + 协议 +参数 
            var protocol = string.Format("{0}/{1}/{2}/{3}", url, nameof(ABServer_Protocol.GetLastUploadFiles), PlayerSettings.applicationIdentifier, platformStr);

            //所有文件的配置
            string ret = "";
            try
            {
                ret = webclient.DownloadString(protocol);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Data);
                return true;
            }

            //获取返回的文件列表
            var response = Response.Cretate(ret);
            if (response.Code == 0)
            {
                Debug.LogError(response.Msg);
                return false;
            }
            var fs        = response.GetContent<List<string>>();
            var fileQueue = new Queue<string>();
            foreach (var f in fs)
            {
                fileQueue.Enqueue(f);
            }

            //删除本地所有素材
            var localPath = string.Format("{0}/{1}/Art", outputPath, platformStr);
            if (Directory.Exists(localPath))
            {
                Directory.Delete(localPath, true);
            }

            Directory.CreateDirectory(localPath);
            //
            int ErrorCont = 0;
            while (fileQueue.Count > 0)
            {
                var filename = fileQueue.Dequeue();
                var furl     = string.Format("{0}/{1}/{2}/{3}/{4}", url, nameof(ABServer_Protocol.Download), PlayerSettings.applicationIdentifier, platformStr, filename);
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
                    if (ErrorCont > 100)
                    {
                        Debug.LogError("失败次数过多，下载失败:" + e.Message);
                        return false;
                    }
                }
            }

            webclient.Dispose();
            Debug.Log("所有ab下载成功!");
            return true;
        }

        /// <summary>
        /// 上传服务器
        /// </summary>
        /// <param name="platform"></param>
        private static void UploadFormFileServer(RuntimePlatform platform)
        {
            var platformStr = BDApplication.GetPlatformPath(platform);
            var url         = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl + "/Assetbundle";
            ;
            var webclient = new WebClient();
            //获取版本号
            var protocol = string.Format("{0}/{1}/{2}/{3}", url, nameof(ABServer_Protocol.GetLastVersion), PlayerSettings.applicationIdentifier, platformStr);
            int version  = 0;
            try
            {
                var ret      = webclient.DownloadString(protocol);
                var response = Response.Cretate(ret);
                version = response.GetContent<int>();
                version++;
            }
            catch (Exception e)
            {
                Debug.LogError("版本号获取错误:" + e.Data);
            }

            //上传
            protocol = string.Format("{0}/{1}/{2}/{3}/{4}", url, nameof(ABServer_Protocol.Upload), PlayerSettings.applicationIdentifier, version, platformStr);
            //获取本地所有素材
            var localPath = string.Format("{0}/{1}/Art", outputPath, platformStr);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            var fs = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories).Where((f) => !f.EndsWith(".meta")).ToList();
            //
            int ErrorCont = 0;
            for (int i = 0; i < fs.Count; i++)
            {
                var f = fs[i];

                try
                {
                    webclient.UploadFile(protocol, f);
                }
                catch (Exception e)
                {
                    //重新压栈执行
                    i--;
                    fs.RemoveAt(0);
                    fs.Add(f);
                    //错误次数
                    ErrorCont++;
                    if (ErrorCont > 10)
                    {
                        Debug.LogError(e.Message);
                        Debug.LogError("失败次数过多，本次任务失败:" + protocol);
                        return;
                    }
                }
            }

            webclient.Dispose();

            Debug.Log("所有ab上传成功,版本号:" + version);
        }

        #endregion
    }
}