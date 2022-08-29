using BDFramework.Core.Tools;
using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;


namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// Assetbundle构建时候的上下文信息
    /// </summary>
    public class AssetBundleBuildingContext
    {
        /// <summary>
        /// 构建参数
        /// </summary>
        public BuildAssetBundleParams BuildParams { get; set; } = new BuildAssetBundleParams();

        /// <summary>
        /// 构建资产的数据
        /// </summary>
        public BuildAssetInfos BuildAssetInfos { get; private set; } = new BuildAssetInfos();
        
        /// <summary>
        /// Runtime资产列表
        /// </summary>
        private List<string> runtimeAssetsPathList = new List<string>();
        
        /// <summary>
        /// runtime下的资源
        /// </summary>
        public List<AssetReference> RuntimeAssetsList { get; private set; } = new List<AssetReference>();

        /// <summary>
        /// Runtime的依赖资源
        /// </summary>
        public List<AssetReference> DependAssetList { get; private set; } = new List<AssetReference>();

        
        /// <summary>
        /// 搜集打包资产信息
        /// </summary>
        public bool CollectBuildAssets(bool isForceReCollect = false)
        {

            //1.生成BuildingAssetInfo信息
            BuildAssetInfos buildInfoCache = null;
            //获取缓存
            if (!this.BuildParams.IsBuilding && !isForceReCollect)
            {
                buildInfoCache = EditorAssetInfosCache.GetBuildingAssetInfosCache();
            }
            
            //获取buildingAssetinfo
            (this.BuildAssetInfos,this.runtimeAssetsPathList) = AssetBundleToolsV2.GetBuildingAssetInfos(buildInfoCache);
            //保存
            EditorAssetInfosCache.SaveBuildingAssetInfosCache(this.BuildAssetInfos);
            
            //2.创建AssetRef
            foreach (var item in this.BuildAssetInfos.AssetInfoMap)
            {
                var arf = AssetReference.CreateReference(item.Key);
                var ret = this.runtimeAssetsPathList.Contains(item.Key);
                if (ret)
                {
                    //创建runtime资源
                    this.RuntimeAssetsList.Add(arf);
                }
                else
                {
                    this.DependAssetList.Add(arf);
                }
            }


            //检测构造的数据
            var count = this.RuntimeAssetsList.Count + this.DependAssetList.Count;
            if (BuildAssetInfos.AssetInfoMap.Count != count)
            {
                Debug.LogErrorFormat("【初始化框架资源环境】出错! buildinfo:{0} output:{1}", BuildAssetInfos.AssetInfoMap.Count, count);

                var tmpBuildAssetsInfo = BuildAssetInfos.Clone();
                foreach (var ra in this.RuntimeAssetsList)
                {
                    tmpBuildAssetsInfo.AssetInfoMap.Remove(ra.importFrom);
                }

                foreach (var drf in this.DependAssetList)
                {
                    tmpBuildAssetsInfo.AssetInfoMap.Remove(drf.importFrom);
                }

                Debug.Log(JsonMapper.ToJson(tmpBuildAssetsInfo.AssetInfoMap, true));

                return false;
            }

            return true;
        }


        #region 打包AssetBundle

        /// <summary>
        /// 开始构建AB
        /// </summary>
        public void StartBuildAssetBundle(BuildTarget buildTarget)
        {
            //-----------------------开始打包AssetBundle逻辑---------------------------
            Debug.Log("【BuildAssetbundle】执行Build...");
            //设置编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.BuildAssetBundle;
            var platform = BApplication.GetRuntimePlatform(buildTarget);
            var platformOutputPath = IPath.Combine(BuildParams.OutputPath, BApplication.GetPlatformPath(platform));
            string abOutputPath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_ROOT_PATH);
            //--------------------------------开始打包----------------------------------
            //1.打包
            Debug.Log("<color=green>----->1.进入打包逻辑</color>");
            //整理ab颗粒度
            BuildAssetInfos.ReorganizeAssetBundleUnit();
            //获取ab列表
            var assetbundleItemList = BuildAssetInfos.GetAssetBundleItems();

            //打包
            AssetDatabase.StartAssetEditing(); //禁止自动导入
            {
                //对比差异文件
                var changedAssetsInfo = AssetBundleToolsV2.GetChangedAssetsByFileHash(this.BuildParams.OutputPath, buildTarget, BuildAssetInfos);
                //打包
                this.BuildAssetBundle(assetbundleItemList, changedAssetsInfo, BuildParams, platform);
            }
            AssetDatabase.StopAssetEditing(); //恢复自动导入


            //2.清理
            Debug.Log("<color=green>----->2.清理旧ab</color>");
            //移除所有的ab
            AssetBundleToolsV2.RemoveAllAssetbundleName();
            //删除本地没有的资源
            var allABList = Directory.GetFiles(abOutputPath, "*", SearchOption.AllDirectories).Where((p) => string.IsNullOrEmpty(Path.GetExtension(p)));
            foreach (var abpath in allABList)
            {
                var abname = Path.GetFileName(abpath);
                var ret = assetbundleItemList.FirstOrDefault((abdata) => abdata.AssetBundlePath == abname);
                if (ret == null)
                {
                    //
                    File.Delete(abpath);
                    File.Delete(abpath + ".manifest");

                    //
                    var path = AssetDatabase.GUIDToAssetPath(abname);
                    Debug.Log("【删除旧ab:】" + abname + "  -  " + path);
                }
            }


            //3.BuildInfo配置处理
            Debug.Log("<color=green>----->3.BuildInfo相关生成</color>");
            //设置ab的hash
            foreach (var abi in assetbundleItemList)
            {
                if (string.IsNullOrEmpty(abi.AssetBundlePath))
                {
                    continue;
                }

                var abpath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_ROOT_PATH, abi.AssetBundlePath);
                var hash = FileHelper.GetMurmurHash3(abpath);
                abi.Hash = hash;
            }

            //获取上一次打包的数据，跟这次打包数据合并
            var configPath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_CONFIG_PATH);
            if (File.Exists(configPath))
            {
                var lastAssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(File.ReadAllText(configPath));
                foreach (var newABI in assetbundleItemList)
                {
                    if (string.IsNullOrEmpty(newABI.AssetBundlePath))
                    {
                        continue;
                    }

                    // //判断是否在当前打包列表中
                    // var ret = changedAssetsInfo.AssetDataMaps.Values.FirstOrDefault((a) => a.ABName.Equals(newABI.AssetBundlePath, StringComparison.OrdinalIgnoreCase));

                    var lastABI = lastAssetbundleItemList.FirstOrDefault((last) =>
                        newABI.AssetBundlePath.Equals(last.AssetBundlePath, StringComparison.OrdinalIgnoreCase)); //AB名相等
                    //&& newABI.Hash == last.Hash); //hash相等
                    //没重新打包，则用上一次的mix信息
                    if (lastABI != null && lastABI.Hash == newABI.Hash)
                    {
                        newABI.Mix = lastABI.Mix;
                    }
                    //否则mix = 0
                }
            }

            //保存artconfig.info
            var csv = CsvSerializer.SerializeToString(assetbundleItemList);
            FileHelper.WriteAllText(configPath, csv);


            //保存BuildInfo配置
            var buildinfoPath = IPath.Combine(platformOutputPath, BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH);
            //缓存buildinfo
            var json = JsonMapper.ToJson(BuildAssetInfos, true);
            FileHelper.WriteAllText(buildinfoPath, json);


            //4.备份Artifacts
            //this.BackupArtifacts(buildTarget);

            //5.检测本地的Manifest和构建预期对比
            Debug.Log("<color=green>----->5.校验AB依赖</color>");
            var abRootPath = IPath.Combine(BuildParams.OutputPath, BApplication.GetPlatformPath(platform), BResources.ART_ASSET_ROOT_PATH);
            var previewABUnitMap = BuildAssetInfos.PreGetAssetbundleUnit();
            var manifestList = Directory.GetFiles(abRootPath, "*.manifest", SearchOption.AllDirectories);
            //解析 manifestBuildParams.OutputPath
            for (int i = 0; i < manifestList.Length; i++)
            {
                var manifest = manifestList[i].Replace("\\", "/");


                if (manifest.Equals(abRootPath + ".manifest"))
                {
                    continue;
                }

                var lines = File.ReadLines(manifest);
                List<string> manifestDependList = new List<string>();
                bool isStartRead = false;
                foreach (var line in lines)
                {
                    if (!isStartRead && line.Equals("Assets:"))
                    {
                        isStartRead = true;
                    }
                    else if (line.Contains("Dependencies:"))
                    {
                        break;
                    }
                    else if (isStartRead)
                    {
                        var file = line.Replace("- ", "");
                        manifestDependList.Add(file.ToLower());
                    }
                }

                //对比依赖
                var abname = Path.GetFileNameWithoutExtension(manifest);
                if (abname.Equals(BResources.ART_ASSET_ROOT_PATH, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                previewABUnitMap.TryGetValue(abname, out var previewABDependList);
                if (previewABDependList == null)
                {
                    Debug.LogError("【AssetbundleV2-验证】本地ab的配置不不存在:" + abname);
                    Debug.LogError("path:" + AssetDatabase.GUIDToAssetPath(abname));
                }
                else
                {
                    //求差集
                    var except = manifestDependList.Except(previewABDependList);
                    if (except.Count() != 0)
                    {
                        var local = JsonMapper.ToJson(manifestDependList, true);
                        var preview = JsonMapper.ToJson(previewABDependList, true);
                        Debug.LogError($"【AssetbundleV2-验证】本地AssetBundle依赖与预期不符:\n 本地:{local} \n 预期:{preview}");
                    }
                }
            }


            //6.资源混淆
            Debug.Log("<color=green>----->6.混淆AB</color>");
            if (BDEditorApplication.BDFrameworkEditorSetting.BuildAssetBundleSetting.IsEnableObfuscation)
            {
                AssetBundleToolsV2.MixAssetBundle(BuildParams.OutputPath, platform);
            }


            //恢复编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.Idle;
            //BD生命周期触发
            BDFrameworkPipelineHelper.OnEndBuildAssetBundle(this);


            //GenAssetBundleItemCacheList = abConfigList.ToList();
        }


        /// <summary>
        /// 打包Asset Bundle
        /// </summary>
        /// <param name="buildAssetInfos"></param>
        /// <param name="buildParams"></param>
        /// <param name="platform"></param>
        private void BuildAssetBundle(List<AssetBundleItem> assetBundleItemList,  List<KeyValuePair<string, BuildAssetInfos.AssetInfo>> buildAssetInfos, BuildAssetBundleParams buildParams, RuntimePlatform platform)
        {
            //----------------------------开始设置build ab name-------------------------------
            //根据传进来的资源,设置AB name
            foreach (var buildInfoItem in buildAssetInfos)
            {
                var assetPath = buildInfoItem.Key;
                var assetData = buildInfoItem.Value;
                //设置ab name
                var ai = AssetBundleToolsV2.GetAssetImporter(assetPath);
                if (ai)
                {
                    ai.SetAssetBundleNameAndVariant(assetData.ABName, null);
                }
            }


            //----------------------------生成AssetBundle-------------------------------
            var platformOutputPath = Path.Combine(buildParams.OutputPath, BApplication.GetPlatformPath(platform));
            string abOutputPath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_ROOT_PATH);
            if (!Directory.Exists(abOutputPath))
            {
                Directory.CreateDirectory(abOutputPath);
            }

            //配置
            var buildTarget = BApplication.GetBuildTarget(platform);
            BuildAssetBundleOptions buildOpa =
                BuildAssetBundleOptions.ChunkBasedCompression | //压缩
                BuildAssetBundleOptions.DeterministicAssetBundle | //保证一致
                //BuildAssetBundleOptions.DisableWriteTypeTree| //关闭TypeTree
                BuildAssetBundleOptions.DisableLoadAssetByFileName | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension; //关闭使用filename加载

            //关闭TypeTree
            var buildAssetConf = BDEditorApplication.BDFrameworkEditorSetting?.BuildAssetBundleSetting;
            if (buildAssetConf.IsDisableTypeTree)
            {
                buildOpa |= BuildAssetBundleOptions.DisableWriteTypeTree; //关闭TypeTree
            }


            UnityEditor.BuildPipeline.BuildAssetBundles(abOutputPath, buildOpa, buildTarget);
            Debug.LogFormat("【编译AssetBundle】 output:{0} ,buildTarget:{1}", abOutputPath, buildTarget.ToString());


            //3.检测本地Assetbundle
            // allAbList = Directory.GetFiles(abOutputPath, "*", SearchOption.AllDirectories);
            // foreach (var abpath in allAbList)
            // {
            //     if (abpath.Contains("."))
            //     {
            //         continue;
            //     }
            //
            //     var depend = AssetDatabase.GetAssetBundleDependencies(abpath,true);
            // }

            GlobalAssetsHelper.GenBasePackageAssetBuildInfo(buildParams.OutputPath, platform);
        }

        #endregion
    }
}
