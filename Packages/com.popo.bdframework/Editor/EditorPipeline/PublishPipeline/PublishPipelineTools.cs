using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.VersionContrller;
using DotNetExtension;
using LitJson;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;


namespace BDFramework.Editor
{
    static public class PublishPipelineTools
    {
        /// <summary>
        /// 资源转hash
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadHttpApi"></param>
        static public void PublishAssetsToServer(string path, string version = "0.0.0")
        {
            var plarforms = new RuntimePlatform[] {RuntimePlatform.Android, RuntimePlatform.IPhonePlayer};


            foreach (var platform in plarforms)
            {
                var platformPath = IPath.Combine(path, BDApplication.GetPlatformPath(platform));
                if (Directory.Exists(platformPath))
                {
                    string versionNum = "testVersion";
                    //发布资源处理前,处理前回调
                    BDFrameworkPublishPipelineHelper.OnBeginPublishAssets(platform, platformPath, out versionNum);
                    //处理资源
                    var outdir = AssetsToHash(path, platform, versionNum);
                    //发布资源处理后,通知回调
                    BDFrameworkPublishPipelineHelper.OnEndPublishAssets(platform, outdir);
                    
                }
            }

            Debug.Log("发布资源处理完成,请继承PublishPipeline生命周期,完成后续自动化处理!");
        }


        /// <summary>
        /// 获取资源hash数据
        /// </summary>
        /// <param name="assetsRootPath"></param>
        /// <returns></returns>
        static public List<ServerAssetItem> GetAssetsHashData(string assetsRootPath, RuntimePlatform platform)
        {
            //黑名单
            List<string> blackFileList = new List<string>()
            {
                BResources.EDITOR_ASSET_BUILD_INFO_PATH,
                BResources.SERVER_ASSETS_SUB_PACKAGE_CONFIG_PATH,
                string.Format("{0}/{0}",BResources.ASSET_ROOT_PATH),
            };

            //加载assetbundle配置
            assetsRootPath = string.Format("{0}/{1}", assetsRootPath, BDApplication.GetPlatformPath(platform));
            var abConfigPath = string.Format("{0}/{1}", assetsRootPath, BResources.ASSET_CONFIG_PATH);
            var abConfigLoader = new AssetbundleConfigLoder();
            abConfigLoader.Load(abConfigPath, null);
            //生成hash配置
            var assets = Directory.GetFiles(assetsRootPath, "*", SearchOption.AllDirectories);
            float count = 0;
            int notABCounter = 1000000;
            //开始生成hash
            var serverAssetItemList = new List<ServerAssetItem>();
            foreach (var assetPath in assets)
            {
                count++;
                EditorUtility.DisplayProgressBar(" 获取资源hash", string.Format("生成文件hash:{0}/{1}", count, assets.Length), count / assets.Length);
                var ext = Path.GetExtension(assetPath).ToLower();
                // bool isConfigFile = false;
                //无效数据
                if (ext == ".manifest" || ext == ".meta")
                {
                    continue;
                }

                //本地的相对路径 
                var localPath = assetPath.Replace("\\", "/").Replace(assetsRootPath + "/", "");

                //黑名单
                var ret = blackFileList.FirstOrDefault((bf) => bf.Equals(localPath, StringComparison.OrdinalIgnoreCase));
                if (ret != null)
                {
                    Debug.Log("【黑名单】剔除:" + ret);
                    continue;
                }

                //文件信息
                var fileHash = FileHelper.GetHashFromFile(assetPath);
                var fileInfo = new FileInfo(assetPath);
                //
                var abpath = Path.GetFileName(assetPath);
                var assetbundleItem = abConfigLoader.AssetbundleItemList.Find((ab) => ab.AssetBundlePath != null && ab.AssetBundlePath == abpath);
                ServerAssetItem item;
                //文件容量
                float fileSize = (int) ((fileInfo.Length / 1024f) * 100f) / 100f;
                //用ab资源id添加
                if (assetbundleItem != null)
                {
                    item = new ServerAssetItem() {Id = assetbundleItem.Id, HashName = fileHash, LocalPath = localPath, FileSize = fileSize};
                }
                else
                {
                    notABCounter++;
                    item = new ServerAssetItem() {Id = notABCounter, HashName = fileHash, LocalPath = localPath, FileSize = fileSize};
                }

                serverAssetItemList.Add(item);
            }

            EditorUtility.ClearProgressBar();
            //按id排序
            serverAssetItemList.Sort((a, b) =>
            {
                if (a.Id < b.Id)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });

            return serverAssetItemList;
        }

        /// <summary>
        /// 文件转hash
        /// </summary>
        /// <param name="assetsRootPath"></param>
        /// <param name="platform"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        static public string AssetsToHash(string assetsRootPath, RuntimePlatform platform, string version)
        {
            //文件夹准备
            var outputDir = string.Format("{0}/{1}", assetsRootPath.Replace("\\", "/"), BDApplication.GetPlatformPath(platform)) + "_ReadyToUpload";
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);

            //获取资源的hash数据
            var allServerAssetItemList = GetAssetsHashData(assetsRootPath, platform);
            foreach (var assetItem in allServerAssetItemList)
            {
                var localpath = string.Format("{0}/{1}/{2}", assetsRootPath, BDApplication.GetPlatformPath(platform), assetItem.LocalPath);
                var copytoPath = string.Format("{0}/{1}", outputDir, assetItem.HashName);
                File.Copy(localpath, copytoPath, true);
            }

            //生成分包信息
            //加载assetbundle配置
            // var abConfigPath = string.Format("{0}/{1}", assetsRootPath, BResources.ASSET_CONFIG_PATH);
            // var abConfigLoader = new AssetbundleConfigLoder();
            // abConfigLoader.Load(abConfigPath, null);
            var path = string.Format("{0}/{1}/{2}", assetsRootPath, BDApplication.GetPlatformPath(platform), BResources.SERVER_ASSETS_SUB_PACKAGE_CONFIG_PATH);
            if (File.Exists(path))
            {
                var subpackageList = CsvSerializer.DeserializeFromString<List<SubPackageConfigItem>>(File.ReadAllText(path));
                foreach (var subPackageConfigItem in subpackageList)
                {
                    var subPackageItemList = new List<ServerAssetItem>();
                    //美术资产
                    foreach (var id in subPackageConfigItem.ArtAssetsIdList)
                    {
                        //var assetbundleItem = abConfigLoader.AssetbundleItemList[id];
                        var serverAssetsItem = allServerAssetItemList.Find((item) => item.Id == id);
                        subPackageItemList.Add(serverAssetsItem);
                    }

                    //脚本
                    foreach (var hcName in subPackageConfigItem.HotfixCodePathList)
                    {
                        var serverAssetsItem = allServerAssetItemList.Find((item) => item.LocalPath == hcName);
                        subPackageItemList.Add(serverAssetsItem);
                    }

                    //表格
                    foreach (var tpName in subPackageConfigItem.TablePathList)
                    {
                        var serverAssetsItem = allServerAssetItemList.Find((item) => item.LocalPath == tpName);
                        subPackageItemList.Add(serverAssetsItem);
                    }

                    //配置表
                    foreach (var ciName in subPackageConfigItem.ConfAndInfoList)
                    {
                        var serverAssetsItem = allServerAssetItemList.Find((item) => item.LocalPath == ciName);
                        subPackageItemList.Add(serverAssetsItem);
                    }

                    //
                    subPackageItemList.Sort((a, b) =>
                    {
                        if (a.Id < b.Id)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    });
                    //写入本地配置
                    var subPackageName = string.Format(BResources.SERVER_ART_ASSETS_SUB_PACKAGE_INFO_PATH, subPackageConfigItem.PackageName);
                    var subPackageInfoPath = IPath.Combine(outputDir, subPackageName);
                    var configContent = CsvSerializer.SerializeToString(subPackageItemList);
                    File.WriteAllText(subPackageInfoPath, configContent);
                    Debug.Log("生成分包文件:" + Path.GetFileName(subPackageInfoPath));
                }
            }

            //生成服务器AssetConfig
            var serverAssetsConfig = new ServerAssetConfig();
            serverAssetsConfig.Platfrom = BDApplication.GetPlatformPath(platform);
            serverAssetsConfig.Version = version;
            var json = JsonMapper.ToJson(serverAssetsConfig);
            var configPath = IPath.Combine(outputDir, BResources.SERVER_ASSETS_VERSION_CONFIG_PATH);
            File.WriteAllText(configPath, json);

            //生成服务器AssetInfo
            var csv = CsvSerializer.SerializeToString(allServerAssetItemList);
            configPath = IPath.Combine(outputDir, BResources.SERVER_ASSETS_INFO_PATH);
            File.WriteAllText(configPath, csv);
            return outputDir;
        }
    }
}
