using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Helper;
using BDFramework.Http;
using Mono.Cecil;
using UnityEngine;

namespace BDFramework.VersionContrller
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
        static async public void Start(string serverConfigPath, string localConfigPath, Action<int, int> onProcess,
            Action<string> onError)
        {
            BDebug.Log("");
            var client = HttpMgr.Inst.GetFreeHttpClient();
            var platform = Utils.GetPlatformPath(Application.platform);
            //开始下载服务器配置
            var serverPath = serverConfigPath + "/" + platform + "/" + platform + "_VersionConfig.json";
            string serverConfig = "";
            try
            {
                serverConfig = await client.DownloadStringTaskAsync(serverPath);
                BDebug.Log("服务器资源配置:" + serverConfig);
            }
            catch (Exception e)
            {
                onError(e.Message);
                return;
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
                    onError(e.Message);
                    return;
                }

                BDebug.Log("下载成功：" + sp);
                onProcess(count, list.Count);
            }

            //写到本地
            if (list.Count > 0)
            {
                File.WriteAllText(localPath, serverConfig);
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
            if (local.Platfrom == server.Platfrom && local.Version < server.Version)
            {
                var assetRoot = root + "/" + Utils.GetPlatformPath(Application.platform);

                var files = Directory.GetFiles(assetRoot, "*.*", SearchOption.AllDirectories);

                HashSet<string> filesSet = new HashSet<string>();
                foreach (var f in files)
                {
                    string _f = f;
                    //windows下路径经常\
                    if (Application.platform == RuntimePlatform.WindowsEditor ||
                        Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        _f = f.Replace("\\", "/");
                    }

                    filesSet.Add(_f.Replace(assetRoot, ""));
                }

                foreach (var item in server.Assets)
                {
                    //本地没有
                    if (filesSet.Contains(item.LocalPath) == false)
                    {
                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}