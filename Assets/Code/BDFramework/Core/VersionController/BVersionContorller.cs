using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BDFramework.Helper;
using BDFramework.Http;
using ILRuntime.Runtime.Intepreter;
using Mono.Cecil;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework
{
    public class AssetConfig
    {
        public string Platfrom = "";
        public double Version = 0.1d;
        public List<AssetItem> Assets = new List<AssetItem>();
    }

    public class AssetItem
    {
        public string HashName = "";
        public string LocalPath = "";
    }

    /// <summary>
    /// 版本控制类
    /// </summary>
    static public class VersionContorller
    {
        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="serverConfigPath">服务器配置根目录</param>
        /// <param name="localConfigPath">本地根目录</param>
        /// <param name="onProcess"></param>
        /// <param name="onError"></param>
        /// 返回码: -1：error  0：success
        static async public Task Start(string serverConfigPath, string localConfigPath, Action<int, int> onProcess,
            Action<string> onError)
        {

            IPath.Combine("", "");
            var client = HttpMgr.Inst.GetFreeHttpClient();
            var platform = Utils.GetPlatformPath(Application.platform);
            //开始下载服务器配置
            var serverPath = serverConfigPath + "/" + platform + "/" + platform + "_VersionConfig.json";
            Debug.Log("server:" + serverPath);
            string serverConfig = "";
            try
            {
                serverConfig = await client.DownloadStringTaskAsync(serverPath);
                BDebug.Log("服务器资源配置:" + serverConfig);
            }
            catch (Exception e)
            {
                onError(e.Message);
            }

            
            var serverconf = LitJson.JsonMapper.ToObject<AssetConfig>(serverConfig);
            AssetConfig localconf = null;
            var localPath = localConfigPath + "/" + platform + "/" + platform + "_VersionConfig.json";

            if (File.Exists(localPath))
            {
                localconf = LitJson.JsonMapper.ToObject<AssetConfig>(File.ReadAllText(localPath));
            }

            //对比差异列表进行下载
            var list = CompareConfig(localConfigPath, localconf, serverconf);
            if (list.Count > 0)
            {
                //预通知要进入热更模式
                onProcess(0, list.Count);
            }

            int count = 0;
            foreach (var item in list)
            {
                count++;
                var sp = serverConfigPath + "/" + platform + "/" + item.HashName;
                var lp = localConfigPath + "/" + platform + "/" + item.LocalPath;

                //创建目录
                var direct = Path.GetDirectoryName(lp);
                if (Directory.Exists(direct) == false)
                {
                    Directory.CreateDirectory(direct);
                }

                //下载
                try
                {
                   await client.DownloadFileTaskAsync(sp, lp);
                }
                catch (Exception e)
                {
                    BDebug.LogError(sp);
                    onError( e.Message);
                }
                

                BDebug.Log("下载成功：" + sp);
                onProcess(count, list.Count);
            }

            //写到本地
            if (list.Count > 0)
            {
                File.WriteAllText(localPath, serverConfig);
            }
            else
            {
                BDebug.Log("可更新数量为0");
            }
            
        }

        static  public IEnumerator IEStart(string serverConfigPath, string localConfigPath, Action<int, int> onProcess, Action<string> onError)
        {

            IPath.Combine("", "");
            var client = HttpMgr.Inst.GetFreeHttpClient();
            var platform = Utils.GetPlatformPath(Application.platform);
            //开始下载服务器配置
            var serverPath = serverConfigPath + "/" + platform + "/" + platform + "_VersionConfig.json";
            Debug.Log("server:" + serverPath);
            string serverConfig = "";
            //下载config
            {
                var wr = UnityWebRequest.Get(serverPath);
                yield return wr.SendWebRequest();
                if (wr.error == null)
                {
                    serverConfig = wr.downloadHandler.text;
                    BDebug.Log("服务器资源配置:" + serverConfig);
                }
                else
                {
                    Debug.LogError(wr.error);
                }

            }


            var serverconf = LitJson.JsonMapper.ToObject<AssetConfig>(serverConfig);
            AssetConfig localconf = null;
            var localPath = localConfigPath + "/" + platform + "/" + platform + "_VersionConfig.json";

            if (File.Exists(localPath))
            {
                localconf = LitJson.JsonMapper.ToObject<AssetConfig>(File.ReadAllText(localPath));
            }

            //对比差异列表进行下载
            var list = CompareConfig(localConfigPath, localconf, serverconf);
            if (list.Count > 0)
            {
                //预通知要进入热更模式
                onProcess(0, list.Count);
            }

            int count = 0;
            foreach (var item in list)
            {
                count++;
                var sp = serverConfigPath + "/" + platform + "/" + item.HashName;
                var lp = localConfigPath + "/" + platform + "/" + item.LocalPath;

                //创建目录
                var direct = Path.GetDirectoryName(lp);
                if (Directory.Exists(direct) == false)
                {
                    Directory.CreateDirectory(direct);
                }

                //下载
                var wr = UnityWebRequest.Get(sp);
                yield return wr.SendWebRequest();
                if (wr.error == null)
                {
                    File.WriteAllBytes(lp, wr.downloadHandler.data);
                    BDebug.Log("下载成功：" + sp);
                    onProcess(count, list.Count);
                }
                else
                {
                    BDebug.LogError("下载失败:" + wr.error);
                    onError(wr.error);
                }
            }

            //写到本地
            if (list.Count > 0)
            {
                File.WriteAllText(localPath, serverConfig);
            }
            else
            {
                BDebug.Log("可更新数量为0");
            }
            
        }
        
        
        
        

        static IEnumerator  IE_DownloadFile(string serverPath, string localPath, Action<bool> callback)
        {
            var wr = UnityWebRequest.Get(serverPath);
            yield return wr;
            if (wr.error == null)
            {
                File.WriteAllBytes(localPath, wr.downloadHandler.data);
                callback(true);
            }
            else
            {
                callback(false);
            }

        }

        /// <summary>
        /// 对比
        /// </summary>
        static public List<AssetItem> CompareConfig(string root, AssetConfig local, AssetConfig server)
        {
            if (local == null)
            {
                return server.Assets;
            }

            var list = new List<AssetItem>();
            //比对平台
            if (local.Platfrom == server.Platfrom ) 
            {
                foreach (var serverAsset in server.Assets)
                {
                    //比较本地是否有 hash、文件名一致的资源
                    var result = local.Assets.Find((a) => a.HashName == serverAsset.HashName && a.LocalPath == serverAsset.LocalPath);
                    
                    if (result == null)
                    {
                        list.Add(serverAsset);
                    }
                }
            }

            return list;
        }
    }
}