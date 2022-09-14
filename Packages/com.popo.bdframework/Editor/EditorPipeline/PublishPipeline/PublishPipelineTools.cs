using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.VersionController;
using DotNetExtension;
using LitJson;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;


namespace BDFramework.Editor
{
    static public class PublishPipelineTools
    {
        static public string UPLOAD_FOLDER_SUFFIX = "_ReadyToUpload";

        /// <summary>
        /// 资源转hash
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadHttpApi"></param>
        static public void PublishAssetsToServer(string path)
        {
            foreach (var platform in BApplication.SupportPlatform)
            {
                //资源路径
                var sourcePath = IPath.Combine(path, BApplication.GetPlatformPath(platform));
                //大概判断原资源是否存在
                if (Directory.Exists(sourcePath))
                {
                    var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                    if (files.Length < 5)
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
                //输出路径
                var outputPath = IPath.Combine(IPath.ReplaceBackSlash(path), UPLOAD_FOLDER_SUFFIX, BApplication.GetPlatformPath(platform));
                if (Directory.Exists(sourcePath))
                {
                    //对比Assets.info 是否一致
                    var sourceAssetsInfoPath = IPath.Combine(sourcePath, BResources.ASSETS_INFO_PATH);
                    var outputAssetsInfoPath = IPath.Combine(outputPath, BResources.ASSETS_INFO_PATH);
                    if (File.Exists(sourceAssetsInfoPath) && File.Exists(outputAssetsInfoPath))
                    {
                        var sourceHash = FileHelper.GetMurmurHash3(sourceAssetsInfoPath);
                        var outputHash = FileHelper.GetMurmurHash3(outputAssetsInfoPath);
                        if (sourceHash == outputHash)
                        {
                            Debug.Log("【PublishPipeline】资源无改动，无需重新生成服务器文件.  -" + BApplication.GetPlatformPath(platform));
                            continue;
                        }
                    }

                    //获取资源版本号
                    var basePckInfo = GlobalAssetsHelper.GetPackageBuildInfo(path, platform);
                    var versionNum = basePckInfo.Version;
                    //发布资源处理前,处理前回调
                    BDFrameworkPipelineHelper.OnBeginPublishAssets(platform, sourcePath, versionNum);
                    //处理资源
                    var outdir = GenServerHashAssets(path, platform, versionNum);
                    //发布资源处理后,通知回调
                    BDFrameworkPipelineHelper.OnEndPublishAssets(platform, outdir, versionNum);
                    Debug.Log("发布资源处理完成! 请继承PublishPipeline生命周期,完成后续自动化部署到自己的文件服务器!");
                }
            }
        }


        /// <summary>
        /// 获取AssetItem信息，这里是所有的资源
        /// </summary>
        /// <param name="assetsRootPath"></param>
        /// <returns></returns>
        static public List<AssetItem> GetAssetItemList(string assetsRootPath, RuntimePlatform platform)
        {
            Debug.Log($"<color=red>------>生成服务器配置:{platform}</color>");
            //黑名单
            List<string> blackFileList = new List<string>()
            {
                BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH,
                BResources.ASSETS_INFO_PATH,
                BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH,
                string.Format("{0}/{0}", BResources.ART_ASSET_ROOT_PATH),
            };
            //混淆文件添加黑名单
            blackFileList.AddRange(AssetBundleToolsV2.GetMixAssets());

            //加载assetbundle配置
            assetsRootPath = IPath.Combine(assetsRootPath, BApplication.GetPlatformPath(platform));
            var abConfigPath = IPath.Combine(assetsRootPath, BResources.ART_ASSET_CONFIG_PATH);
            var abTypeConfigPath = IPath.Combine(assetsRootPath, BResources.ART_ASSET_TYPES_PATH);
            var abConfigLoader = new AssetbundleConfigLoder();
            abConfigLoader.Load(abConfigPath, abTypeConfigPath);
            //生成hash配置
            var assets = Directory.GetFiles(assetsRootPath, "*", SearchOption.AllDirectories);
            int ABCounter = 0;
            int notABCounter = 1000000;
            //开始生成hash
            var serverAssetItemList = new List<AssetItem>();

            for (int i = 0; i < assets.Length; i++)
            {
                var assetPath = assets[i];

                EditorUtility.DisplayProgressBar(" 获取资源hash", string.Format("生成文件hash:{0}/{1}", i, assets.Length), i / assets.Length);
                var ext = Path.GetExtension(assetPath).ToLower();
                //无效数据
                if (ext == ".manifest" || ext == ".meta")
                {
                    continue;
                }

                //本地的相对路径 
                var localPath = assetPath.Replace("\\", "/").Replace(assetsRootPath + "/", "");

                //黑名单
                var ret = blackFileList.FirstOrDefault((bf) => localPath.Equals(bf, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(localPath).Equals(bf));
                if (ret != null)
                {
                    Debug.Log("【黑名单】剔除:" + ret);
                    continue;
                }

                //文件信息
                var fileHash = FileHelper.GetMurmurHash3(assetPath);
                var abPath = Path.GetFileName(assetPath);
                var assetbundleItem = abConfigLoader.AssetbundleItemList.Find((ab) => ab.AssetBundlePath != null && ab.AssetBundlePath.Equals(abPath));
                AssetItem item = null;
                //文件容量
                var fileInfo = new FileInfo(assetPath);
                float fileSize = (int) ((fileInfo.Length / 1024f) * 100f) / 100f;
                //用ab资源id添加
                if (assetbundleItem != null)
                {
                    if (assetbundleItem.Hash == fileHash)
                    {
                        ABCounter++;
                        //这里使用ab的id,用以分包寻找资源
                        item = new AssetItem() {Id = assetbundleItem.Id, HashName = fileHash, LocalPath = localPath, FileSize = fileSize};
                    }
                    else
                    {
                        Debug.LogError("【ServerAssetsItem.Info】错误! AssetBundle hash不匹配!");
                    }
                }
                else
                {
                    notABCounter++;
                    item = new AssetItem() {Id = notABCounter, HashName = fileHash, LocalPath = localPath, FileSize = fileSize};
                }

                serverAssetItemList.Add(item);
                //放在最后，避免提前return 占用id
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
        /// <param name="outputRootPath"></param>
        /// <param name="platform"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        static public string GenServerHashAssets(string outputRootPath, RuntimePlatform platform, string version)
        {
            Debug.Log($"<color=red>------>生成服务器Hash文件:{BApplication.GetPlatformPath(platform)}</color>");
            outputRootPath = IPath.ReplaceBackSlash(outputRootPath);
            //文件夹准备
            var outputDir = IPath.Combine(outputRootPath, UPLOAD_FOLDER_SUFFIX, BApplication.GetPlatformPath(platform));
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);
            //获取资源的hash数据
            var assetItemList = GetAssetItemList(outputRootPath, platform);
            foreach (var assetItem in assetItemList)
            {
                var localpath = IPath.Combine(outputRootPath, BApplication.GetPlatformPath(platform), assetItem.LocalPath);
                var copytoPath = IPath.Combine(outputDir, assetItem.HashName);
                File.Copy(localpath, copytoPath);
            }

            //服务器版本信息
            var serverAssetsInfo = new AssetsVersionInfo();
            //生成分包信息
            //加载assetbundle配置
            // var abConfigPath = string.Format("{0}/{1}", assetsRootPath, BResources.ASSET_CONFIG_PATH);
            // var abConfigLoader = new AssetbundleConfigLoder();
            // abConfigLoader.Load(abConfigPath, null);
            var path = IPath.Combine(outputRootPath, BApplication.GetPlatformPath(platform), BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH);
            if (File.Exists(path))
            {
                var subpackageList = CsvSerializer.DeserializeFromString<List<SubPackageConfigItem>>(File.ReadAllText(path));
                foreach (var subPackageConfigItem in subpackageList)
                {
                    var subPackageItemList = new List<AssetItem>();
                    //美术资产
                    foreach (var id in subPackageConfigItem.ArtAssetsIdList)
                    {
                        //var assetbundleItem = abConfigLoader.AssetbundleItemList[id];
                        var serverAssetsItem = assetItemList.Find((item) => item.Id == id);
                        subPackageItemList.Add(serverAssetsItem);

                        if (serverAssetsItem == null)
                        {
                            Debug.LogError("不存在art asset:" + id);
                        }
                    }

                    //脚本
                    foreach (var hcName in subPackageConfigItem.HotfixCodePathList)
                    {
                        var serverAssetsItem = assetItemList.Find((item) => item.LocalPath == hcName);
                        subPackageItemList.Add(serverAssetsItem);
                        if (serverAssetsItem == null)
                        {
                            Debug.LogError("不存在code asset:" + hcName);
                        }
                    }

                    //表格
                    foreach (var tpName in subPackageConfigItem.TablePathList)
                    {
                        var serverAssetsItem = assetItemList.Find((item) => item.LocalPath == tpName);
                        subPackageItemList.Add(serverAssetsItem);

                        if (serverAssetsItem == null)
                        {
                            Debug.LogError("不存在table asset:" + tpName);
                        }
                    }

                    //配置
                    foreach (var confName in subPackageConfigItem.ConfAndInfoList)
                    {
                        var serverAssetsItem = assetItemList.Find((item) => item.LocalPath == confName);
                        subPackageItemList.Add(serverAssetsItem);
                        if (serverAssetsItem == null)
                        {
                            Debug.LogError("不存在conf:" + confName);
                        }
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
                    var subPackageInfoPath = BResources.GetAssetsSubPackageInfoPath( IPath.Combine(outputRootPath, UPLOAD_FOLDER_SUFFIX), platform, subPackageConfigItem.PackageName);
                    var configContent = CsvSerializer.SerializeToString(subPackageItemList);
                    FileHelper.WriteAllText(subPackageInfoPath, configContent);
                    Debug.Log("生成分包文件:" + Path.GetFileName(subPackageInfoPath));
                    //写入subPck - version
                    serverAssetsInfo.SubPckMap[Path.GetFileName(subPackageInfoPath)] = version;
                }
            }

            //生成服务器AssetInfo
            var csv = CsvSerializer.SerializeToString(assetItemList);
            var configPath = IPath.Combine(outputDir, BResources.ASSETS_INFO_PATH);
            FileHelper.WriteAllText(configPath, csv);

            //生成服务器版本号
            serverAssetsInfo.Platfrom = BApplication.GetPlatformPath(platform);
            serverAssetsInfo.Version = version;
            var json = JsonMapper.ToJson(serverAssetsInfo);
            configPath = IPath.Combine(outputDir, BResources.SERVER_ASSETS_VERSION_INFO_PATH);
            FileHelper.WriteAllText(configPath, json);

            return outputDir;
        }
    }
}
