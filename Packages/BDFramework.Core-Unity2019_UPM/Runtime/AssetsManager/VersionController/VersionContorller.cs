using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Code.BDFramework.Core.Tools;
using UnityEngine;
using UnityEngine.Networking;

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


    public enum UpdateMode
    {
        CompareVersionConfig, //对比版本文件
        Repair,               //修复模式,对比本地文件
    }
    
    
    /// <summary>
    /// 版本控制类
    /// </summary>
    static public class VersionContorller
    {
        private static List<AssetItem> curDownloadList = null;
        private static int curDonloadIndex = 0;
        private static string serverConfig = null;

        private static string localConfigRootPath = null;

        static public void Start(UpdateMode mode ,string serverConfigPath, string localConfigPath, Action<int, int> onProcess,Action<string> onError)
        {
            IEnumeratorTool.StartCoroutine(IE_Start(mode,serverConfigPath, localConfigPath, onProcess, onError));
        }

        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="serverConfigPath">服务器配置根目录</param>
        /// <param name="localConfigPath">本地根目录</param>
        /// <param name="onProcess"></param>
        /// <param name="onError"></param>
        /// 返回码: -1：error  0：success
        static private IEnumerator IE_Start(UpdateMode mode,string serverConfigPath, string localConfigPath, Action<int, int> onProcess, Action<string> onError)
        {

            localConfigRootPath = localConfigPath;
            
            var platform = BDApplication.GetPlatformPath(Application.platform);
            
            if (curDownloadList == null || curDownloadList.Count == 0)
            {
                //开始下载服务器配置
                var serverPath = serverConfigPath + "/" + platform + "/" + platform + "_VersionConfig.json";
                BDebug.Log("server:" + serverPath);
               
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
                var localPath = string.Format("{0}/{1}/{2}_VersionConfig.json", localConfigPath, platform, platform);

                if (File.Exists(localPath))
                {
                    localconf = LitJson.JsonMapper.ToObject<AssetConfig>(File.ReadAllText(localPath));
                }

                //对比差异列表进行下载
                //不同模式生成不同下载列表
                switch (mode)
                {
                    case  UpdateMode.CompareVersionConfig:
                        curDownloadList = CompareVersionConfig(localconf, serverconf);
                        break;
                    case  UpdateMode.Repair:

                        curDownloadList = Repair(localconf, serverconf);
                        break;
                }
            
                if (curDownloadList.Count > 0)
                {
                    //预通知要进入热更模式
                    onProcess(0, curDownloadList.Count);
                }
            }


            while (curDonloadIndex< curDownloadList.Count)
            {

                var item = curDownloadList[curDonloadIndex];

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
                    onProcess(curDonloadIndex,  curDownloadList.Count -1);
                }
                else
                {
                    BDebug.LogError("下载失败:" + wr.error);
                    onError(wr.error);          
                    yield break;
                }

                //自增
                curDonloadIndex++;
            }
            
            //写到本地
            if ( curDownloadList.Count > 0)
            {
                File.WriteAllText( string.Format("{0}/{1}/{2}_VersionConfig.json", localConfigPath, platform, platform), serverConfig);
            }
            else
            {
                BDebug.Log("不用更新");
                onProcess(1, 1);
            }
            
            
            //重置
            curDownloadList = null;
            curDonloadIndex = 0;
            serverConfig = null;
        }
        
       
        /// <summary>
        /// 对比版本配置
        /// </summary>
        static public List<AssetItem> CompareVersionConfig(AssetConfig local, AssetConfig server)
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
        
        
        /// <summary>
        /// 修复模式,是要对比本地文件是否存在
        /// </summary>
        static public List<AssetItem> Repair(AssetConfig local, AssetConfig server)
        {
            if (local == null)
            {

                return server.Assets;
            }

            var list = new List<AssetItem>();
            //比对平台
            if (local.Platfrom == server.Platfrom ) 
            {
                //平台
                var platform = BDApplication.GetPlatformPath(Application.platform);
                //
                foreach (var serverAsset in server.Assets)
                {
                    //比较本地是否有 hash、文件名一致的资源
                    var result = local.Assets.Find((a) => a.HashName == serverAsset.HashName && a.LocalPath == serverAsset.LocalPath);
                    //配置不存在
                    if (result == null)
                    {
                        list.Add(serverAsset);
                    }
                    else
                    {
                        //配置存在，判断文件存不存在,存在还要判断hash         
                        var fs =localConfigRootPath + "/" + platform + "/" + serverAsset.LocalPath;
                        if (!File.Exists(fs))
                        {
                            list.Add(serverAsset);
                        }
                    }
                }
            }

            return list;
        }
        
    }
}