using System;
using System.Collections;
using System.IO;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using Code.BDFramework.Core.Tools;
using LitJson;
using UnityEngine;

namespace BDFramework.AssetHelper
{
    public class PackageBuildInfo
    {
        /// <summary>
        /// 构建时间
        /// </summary>
        public long BuildTime = 0;
    }

    /// <summary>
    /// 资源辅助类，用以统一管理Sql、dll、和ArtConfig资源
    /// </summary>
    static public class AssetHelper
    {
        static private string PackageBuildInfo = "PackageBuild.Info";

        /// <summary>
        /// 检测StreamingAsset下的资源包版本
        /// StreamingAsset 和 Persistent对比
        /// </summary>
        /// <param name="callback"></param>
        static public void CheckAssetPackageVersion(RuntimePlatform platform,Action callback)
        {

            IEnumeratorTool.StartCoroutine(IECheckAssetPackageVersion(platform, callback));
        }

        /// <summary>
        /// 生成资源包版本
        /// </summary>
        static public void GenPackageBuildInfo(string ouptputPath,RuntimePlatform platform)
        {
           var path =  string.Format("{0}/{1}/{2}", ouptputPath, BDApplication.GetPlatformPath(platform),
                PackageBuildInfo);
           //写入buildinfo内容
           var buildinfo = new PackageBuildInfo();
           buildinfo.BuildTime = DateTime.Now.ToFileTime();
           var content = JsonMapper.ToJson(buildinfo);
           //写入本地
           FileHelper.WriteAllText(path,content);
        }

        /// <summary>
        /// 拷贝文件到Persistent下
        /// </summary>
        /// <returns></returns>
        static private IEnumerator IECheckAssetPackageVersion(RuntimePlatform platform,Action callback)
        {
            var persistent = string.Format("{0}/{1}", Application.persistentDataPath, BDApplication.GetPlatformPath(platform));
            var streamingAsset = string.Format("{0}/{1}", Application.streamingAssetsPath, BDApplication.GetPlatformPath(platform));

            var persistentPackageInfoPath = string.Format("{0}/{1}", persistent, PackageBuildInfo);
            var  streamingPackageinfoPath =  string.Format("{0}/{1}", streamingAsset, PackageBuildInfo);
            WWW www = new WWW(streamingPackageinfoPath);
            yield return www;
            if (www.error != null)
            {
                //不存在Streaming配置
                callback?.Invoke();
                yield break;
            }
            else
            {
                //判断版本
                if (File.Exists(persistentPackageInfoPath))
                {
                    var content = File.ReadAllText(persistentPackageInfoPath);
                    var persistentPackageInfo = JsonMapper.ToObject<PackageBuildInfo>(content);
                    var streamingPackageInfo = JsonMapper.ToObject<PackageBuildInfo>(www.text);

                    if (persistentPackageInfo.BuildTime <= streamingPackageInfo.BuildTime)
                    {
                        callback?.Invoke();
                        yield break;
                    }
                    else
                    {
                        //Streaming版本比较新
                        //复制Stream的packageinfo 到persistent
                        File.WriteAllBytes(persistentPackageInfoPath,www.bytes);
                    }
                }
                else
                {
                    //persistent版本不存在
                    //复制Stream的packageinfo 到persistent
                    File.WriteAllBytes(persistentPackageInfoPath,www.bytes);
                }
            }
            //复制新版本的DLL 
            var persistentDLLPath = string.Format("{0}/{1}", persistent, ScriptLoder.DLLPATH);
            var  streamingDLLPath =  string.Format("{0}/{1}", streamingAsset, ScriptLoder.DLLPATH);
            www = new WWW(streamingDLLPath);
            yield return www;
            if (www.error == null)
            {
                File.WriteAllBytes(persistentDLLPath,www.bytes);
            }
            //复制Sql
            var persistentSQLPath = string.Format("{0}/{1}", persistent, SqliteLoder.DBPATH);
            var  streamingSQLPath =  string.Format("{0}/{1}", streamingAsset, SqliteLoder.DBPATH);
            www = new WWW(streamingSQLPath);
            yield return www;
            if (www.error == null)
            {
                File.WriteAllBytes(persistentSQLPath,www.bytes);
            }
            //复制ArtConfig
            var persistentArtConfigPath = string.Format("{0}/{1}", persistent, BResources.CONFIGPATH);
            var  streamingArtConfigPath =  string.Format("{0}/{1}", streamingAsset,  BResources.CONFIGPATH);
            www = new WWW(streamingArtConfigPath);
            yield return www;
            if (www.error == null)
            {
                File.WriteAllBytes(persistentArtConfigPath,www.bytes);
            }
            callback?.Invoke();
            
            yield return null;
        }
    }
}