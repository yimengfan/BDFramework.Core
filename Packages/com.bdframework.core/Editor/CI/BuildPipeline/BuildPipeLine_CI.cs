using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BDFramework.Editor.Asset;
using BDFramework.Editor.EditorLife;
using BDFramework.Core.Tools;
using BDFramework.Editor;
using LitJson;
using UnityEditor;
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

            return (T) new object();
        }
    }

    static public class BuildPipeLine_CI
    {
        private static string outputPath = "";

        static BuildPipeLine_CI()
        {
            //初始化编辑器
            BDFrameEditorLife.InitFrameEditor();
            //
            outputPath = BDApplication.ProjectRoot+"/CI_TEMP";
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
            //下载
            DownloadFormFileServer(RuntimePlatform.IPhonePlayer);
            //构建
            var ret = BuildAssetBundle(RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
            if (ret)
            {
                //执行打包后上传
                UploadFormFileServer(RuntimePlatform.IPhonePlayer);
            }
            else
            {
                Debug.LogError("未有资源变动，无需上传!");
            }
            //删除目录防止下次导入
            var localPath = string.Format("{0}/{1}/Art", outputPath, BDApplication.GetPlatformPath(RuntimePlatform.IPhonePlayer));
            Directory.Delete(localPath,true);
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
            if (ret)
            {
                //执行打包后上传
                UploadFormFileServer(RuntimePlatform.Android);
            }
            else
            {
                Debug.LogError("未有资源变动，无需上传!");
            }
            //删除目录防止下次导入
            var localPath = string.Format("{0}/{1}/Art", outputPath, BDApplication.GetPlatformPath(RuntimePlatform.Android));
            Directory.Delete(localPath,true);
            
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
            BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath,   BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.Android);
        }

        #endregion

        #region 发布资源版本

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
                EditorBuildPackage.BuildAPK(buildMode);
                //上传APK
                UploadAPK();
            }
            else if (platform == RuntimePlatform.IPhonePlayer)
            {
                //测试构建
               // BuildTest.BuildIPA();
                //EditorBuildPackage.BuildIpa();
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
            GetLastVersion,
            UploadAPK,
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

        #region 上传APK
        
        private static void UploadAPK()
        {
            var outdir = BDApplication.ProjectRoot + "/Build";
            var apkPath = IPath.Combine(  outdir,  Application.productName+".apk");
            if (!File.Exists(apkPath))
            {
                Debug.LogError("不存在APK文件!!");
                throw new Exception("不存在APK文件!!");
                return;   
            }
            var url = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl + "/APK";
            var protocol = $"{url}/{nameof(ABServer_Protocol.UploadAPK)}";
            var webclient = new WebClient();
            int maxErrorCount = 10;
            bool isSuccess = true;
            for (int i = 0; i < maxErrorCount; i++)
            {
                try
                {
                    webclient.UploadFile(protocol, apkPath);
                }
                catch (Exception e)
                {
                    if (i == maxErrorCount - 1)
                    {
                        Debug.LogError(e.Message);
                        Debug.Log($"失败次数过多 {protocol}");
                        throw new Exception("失败次数过多 {protocol}!!");
                        isSuccess = false;
                    }
                }
            }
            webclient.Dispose();
            if (isSuccess)
            {
                Debug.Log("APK上传成功!");
            }
        }

        #endregion
    }
}