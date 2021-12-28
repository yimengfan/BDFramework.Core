using System;
using System.Collections;
using System.IO;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.Core.Tools;
using Cysharp.Text;
using LitJson;
using UnityEngine;

namespace BDFramework.Asset
{
    /// <summary>
    /// 包体构建信息
    /// </summary>
    public class PackageBuildInfo
    {
        /// <summary>
        /// 构建时间
        /// </summary>
        public long BuildTime = 0;

        /// <summary>
        /// AB构建svc版本号
        /// </summary>
        public string AssetBundleSVCVersion = "";

        /// <summary>
        /// 脚本构建svc版本号
        /// </summary>
        public string ScriptSVCVersion = "";

        /// <summary>
        /// 表格构建svc版本号
        /// </summary>
        public string TableSVCVersion = "";
    }

    /// <summary>
    /// 资源辅助类，用以统一管理Sql、dll、和ArtConfig资源
    /// </summary>
    static public class GameAssetHelper
    {
        /// <summary>
        /// 检测StreamingAsset下的资源包版本
        /// StreamingAsset 和 Persistent对比
        /// </summary>
        /// <param name="callback"></param>
        static public void CheckAssetPackageVersion(RuntimePlatform platform, Action callback)
        {
            switch (platform)
            {
                //android沙盒用www访问
                case RuntimePlatform.Android:
                {
                    IEnumeratorTool.StartCoroutine(AndroidCheckAssetPackageVersion(platform, callback));
                }
                    break;
                default:
                {
                    IOSCheckAssetPackageVersion(platform, callback);
                }
                    break;
            }
        }

        /// <summary>
        /// 生成资源包构建信息
        /// </summary>
        static public void GenPackageBuildInfo(string ouptputPath, RuntimePlatform platform, string assetSVC = "", string scriptSVC = "", string tableSVC = "")
        {
            var path = string.Format("{0}/{1}/{2}", ouptputPath, BDApplication.GetPlatformPath(platform), BResources.PACKAGE_BUILD_INFO_PATH);

            //写入buildinfo内容
            var buildinfo = new PackageBuildInfo();
            DateTime startTime = TimeZoneInfo.ConvertTime(new System.DateTime(1970, 1, 1), TimeZoneInfo.Utc, TimeZoneInfo.Local); // 当地时区
            long timeStamp = (long) (DateTime.Now - startTime).TotalSeconds;
            buildinfo.BuildTime = timeStamp;
            var content = JsonMapper.ToJson(buildinfo);
            //写入本地
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileHelper.WriteAllText(path, content);
        }

        /// <summary>
        /// 母包资源检测逻辑
        /// </summary>
        /// <returns></returns>
        static private IEnumerator AndroidCheckAssetPackageVersion(RuntimePlatform platform, Action callback)
        {
            BDebug.Log("【资源包】执行母包资源检测逻辑！");
            //路径初始化
            var targetPath = string.Format("{0}/{1}", Application.persistentDataPath, BDApplication.GetPlatformPath(platform));
            string source = "";
            if (Application.isEditor)
            {
                //编辑器下 从加载目标拷贝
                source = GameConfig.GetLoadPath(BDLauncher.Inst.GameConfig.ArtRoot);
            }
            else
            {
                source = Application.streamingAssetsPath;
            }

            var sourcePath = string.Format("{0}/{1}", source, BDApplication.GetPlatformPath(platform));
            //PackageInfo
            var persistentPackageInfoPath = string.Format("{0}/{1}", targetPath, BResources.PACKAGE_BUILD_INFO_PATH);
            var streamingPackageinfoPath = string.Format("{0}/{1}", sourcePath, BResources.PACKAGE_BUILD_INFO_PATH);
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
                        BDebug.Log("【母包资源检测】不复制，Streaming无新资源");
                        callback?.Invoke();
                        yield break;
                    }
                    else
                    {
                        BDebug.Log("【母包资源检测】复制，Streaming有新资源");
                        //Streaming版本比较新
                        //复制Stream的packageinfo 到persistent
                        FileHelper.WriteAllBytes(persistentPackageInfoPath, www.bytes);
                    }
                }
                else
                {
                    BDebug.Log("【母包资源检测】第一次创建资源包info到persistent目录");
                    //persistent版本不存在
                    //复制Stream的packageinfo 到persistent
                    FileHelper.WriteAllBytes(persistentPackageInfoPath, www.bytes);
                }
            }

            //要拷贝的资源
            string[] copyFiles = new string[]
            {
                ScriptLoder.DLL_PATH, ScriptLoder.DLL_PATH + ".pdb", //Dll
                SqliteLoder.LOCAL_DB_PATH, //db
                BResources.ASSET_CONFIG_PATH, BResources.ASSET_TYPES_PATH, //ArtConfig
            };
            //开始拷贝逻辑
            for (int i = 0; i < copyFiles.Length; i++)
            {
                //拷贝逻辑
                var copyFile = copyFiles[i];
                var persistentPath = string.Format("{0}/{1}", targetPath, copyFile);
                var streamingPath = string.Format("{0}/{1}", sourcePath, copyFile);
                www = new WWW(streamingPath);
                yield return www;
                if (www.error == null)
                {
                    FileHelper.WriteAllBytes(persistentPath, www.bytes);
                    BDebug.Log("【母包资源检测】复制成功:" + copyFile);
                }
                else
                {
                    BDebug.LogError("【母包资源检测】复制失败:" + copyFile);
                }
            }

            yield return null;

            callback?.Invoke();
        }

        /// <summary>
        /// 母包资源检测逻辑
        /// </summary>
        /// <returns></returns>
        static private void IOSCheckAssetPackageVersion(RuntimePlatform platform, Action callback)
        {
            BDebug.Log("【资源包】执行母包资源检测逻辑！");
            //路径初始化
            var targetPath = string.Format("{0}/{1}", Application.persistentDataPath, BDApplication.GetPlatformPath(platform));
            string source = "";
            if (Application.isEditor)
            {
                //编辑器下 从加载目标拷贝
                source = GameConfig.GetLoadPath(BDLauncher.Inst.GameConfig.ArtRoot);
                if (source == Application.persistentDataPath)
                {
                    var s1= ZString.Format("{0}/{1}", Application.streamingAssetsPath, BDApplication.GetPlatformPath(platform));
                    if (Directory.Exists(s1))
                    {
                        source = Application.streamingAssetsPath;
                    }
                    else
                    {
                        var s2 =ZString.Format("{0}/{1}", BDApplication.DevOpsPublishAssetsPath, BDApplication.GetPlatformPath(platform));
                        if (Directory.Exists(s2))
                        {
                            source = BDApplication.DevOpsPublishAssetsPath;
                        }
                        else
                        {
                            Debug.LogError("【资源包】本地无资源,可能逻辑出错,请检查!");
                        }
                    }
                }
            }
            else
            {
                source = Application.streamingAssetsPath;
            }

            var sourcePath = ZString.Format("{0}/{1}", source, BDApplication.GetPlatformPath(platform));

            //packageinfo
            var persistentPackageInfoPath = string.Format("{0}/{1}", targetPath, BResources.PACKAGE_BUILD_INFO_PATH);
            var streamingPackageinfoPath = string.Format("{0}/{1}", sourcePath, BResources.PACKAGE_BUILD_INFO_PATH);
            if (!File.Exists(streamingPackageinfoPath))
            {
                //不存在Streaming配置
                BDebug.LogError("【资源包】拷贝失败,不存在：" + streamingPackageinfoPath);
                callback?.Invoke();
            }
            else
            {
                var streamingPackageInfoContent = File.ReadAllText(streamingPackageinfoPath);
                //persitent存在，判断版本
                if (File.Exists(persistentPackageInfoPath))
                {
                    var content = File.ReadAllText(persistentPackageInfoPath);
                    var persistentPackageInfo = JsonMapper.ToObject<PackageBuildInfo>(content);

                    var streamingPackageInfo = JsonMapper.ToObject<PackageBuildInfo>(streamingPackageInfoContent);
                    if (persistentPackageInfo.BuildTime >= streamingPackageInfo.BuildTime)
                    {
                        //跳出，检测结束
                        BDebug.Log("【母包资源检测】不复制，Streaming 无新资源");
                        callback?.Invoke();

                        return;
                    }
                    else
                    {
                        BDebug.Log("【母包资源检测】复制，Streaming 有新资源");
                        //Streaming版本比较新
                        //复制Stream的packageinfo 到persistent
                        FileHelper.WriteAllText(persistentPackageInfoPath, streamingPackageInfoContent);
                    }
                }
                else
                {
                    BDebug.Log("【母包资源检测】第一次创建资源包info到persistent目录");
                    //persistent版本不存在
                    //复制Stream的packageinfo 到persistent
                    FileHelper.WriteAllText(persistentPackageInfoPath, streamingPackageInfoContent);
                }
            }

            //要拷贝的资源
            string[] copyFiles = new string[]
            {
                ScriptLoder.DLL_PATH, ScriptLoder.DLL_PATH + ".pdb", //Dll
                SqliteLoder.LOCAL_DB_PATH, //db
                BResources.ASSET_CONFIG_PATH, BResources.ASSET_TYPES_PATH, //ArtConfig
            };

            //开始拷贝逻辑
            for (int i = 0; i < copyFiles.Length; i++)
            {
                var copyFile = copyFiles[i];
                //复制新版本的DLL 
                var persistentPath = string.Format("{0}/{1}", targetPath, copyFile);
                var streamingPath = string.Format("{0}/{1}", sourcePath, copyFile);

                if (File.Exists(streamingPath))
                {
                    FileHelper.WriteAllBytes(persistentPath, File.ReadAllBytes(streamingPath));
                    BDebug.Log("【母包资源检测】复制成功:" + copyFile);
                }
                else
                {
                    BDebug.LogError("【母包资源检测】复制失败:" + copyFile);
                }
            }

            //结束
            callback?.Invoke();
        }
    }
}
