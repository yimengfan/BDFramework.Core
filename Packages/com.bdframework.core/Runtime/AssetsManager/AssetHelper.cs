using System;
using System.Collections;
using System.IO;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.Core.Tools;
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
           DateTime startTime = TimeZoneInfo.ConvertTime(new System.DateTime(1970, 1, 1), TimeZoneInfo.Utc,TimeZoneInfo.Local);  // 当地时区
           long timeStamp = (long)(DateTime.Now - startTime).TotalSeconds;
           buildinfo.BuildTime = timeStamp;
           var content = JsonMapper.ToJson(buildinfo);
           //写入本地
           if (File.Exists(path))
           {
               File.Delete(path);
           }
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
                    if (persistentPackageInfo.BuildTime >= streamingPackageInfo.BuildTime)
                    {
                        callback?.Invoke();
                        BDebug.Log("【资源包】不复制，persistent目录最新");
                        yield break;
                    }
                    else
                    {
                        BDebug.Log("【资源包】复制，Streaming包info更新");
                        //Streaming版本比较新
                        //复制Stream的packageinfo 到persistent
                        FileHelper.WriteAllBytes(persistentPackageInfoPath,www.bytes);
                    }
                }
                else
                {
                    BDebug.Log("【资源包】第一次创建资源包info到persistent目录");
                    //persistent版本不存在
                    //复制Stream的packageinfo 到persistent
                    FileHelper.WriteAllBytes(persistentPackageInfoPath,www.bytes);
                }
            }
            //复制新版本的DLL 
            var persistentDLLPath = string.Format("{0}/{1}", persistent, ScriptLoder.DLLPATH);
            var  streamingDLLPath =  string.Format("{0}/{1}", streamingAsset, ScriptLoder.DLLPATH);
            www = new WWW(streamingDLLPath);
            yield return www;
            if (www.error == null)
            {
                FileHelper.WriteAllBytes(persistentDLLPath,www.bytes);
                BDebug.Log("复制dll成功!");
            }
            
            www = new WWW(streamingDLLPath+".pdb");
            yield return www;
            if (www.error == null)
            {
                FileHelper.WriteAllBytes(persistentDLLPath+".pdb",www.bytes);
                BDebug.Log("复制dll.pdb成功!");
            }
            else
            {
                //删除persistent下的pdb防止跟dll不匹配
                var pbdPath= persistentDLLPath +".pdb";
                if(File.Exists(pbdPath))
                {
                    File.Delete(pbdPath);
                }
            }
            //复制Sql
            var persistentSQLPath = string.Format("{0}/{1}", persistent, SqliteLoder.DBPATH);
            var  streamingSQLPath =  string.Format("{0}/{1}", streamingAsset, SqliteLoder.DBPATH);
            www = new WWW(streamingSQLPath);
            yield return www;
            if (www.error == null)
            {
                FileHelper.WriteAllBytes(persistentSQLPath,www.bytes);
                BDebug.Log("复制db成功!");
            }
            //复制ArtConfig
            var persistentArtConfigPath = string.Format("{0}/{1}", persistent, BResources.CONFIGPATH);
            var  streamingArtConfigPath =  string.Format("{0}/{1}", streamingAsset,  BResources.CONFIGPATH);
            www = new WWW(streamingArtConfigPath);
            yield return www;
            if (www.error == null)
            {
                FileHelper.WriteAllBytes(persistentArtConfigPath,www.bytes);
                BDebug.Log("复制artconfig成功!");
            }
            callback?.Invoke();
            
            yield return null;
        }
    }
}