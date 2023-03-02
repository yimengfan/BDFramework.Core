using BDFramework.Core.Tools;
using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using ServiceStack.Text;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.AssetGraph;
using BuildCompression = UnityEngine.BuildCompression;
using Debug = UnityEngine.Debug;


namespace BDFramework.Editor.BuildPipeline.AssetBundle
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
        /// Assetbundle列表
        /// </summary>
        public List<AssetBundleItem> AssetBundleItemList { get; private set; }

        /// <summary>
        /// 搜集打包资产信息
        /// </summary>
        public bool CollectBuildAssets(string[] assetDirectories, bool isForceReCollect = false)
        {
            //1.  获取缓存
            BuildAssetInfos buildInfoCache = null;
            bool isGetCache = !this.BuildParams.IsBuilding && !isForceReCollect;
            if (isGetCache)
            {
                buildInfoCache = BuildPipelineAssetCacheImporter.GetBuildingAssetInfosCache();
            }

            //获取buildingAssetinfo
            (this.BuildAssetInfos, this.runtimeAssetsPathList) = AssetBundleToolsV2.GetBuildingAssetInfos(assetDirectories, buildInfoCache);

            //保存会cache
            BuildPipelineAssetCacheImporter.SaveBuildingAssetInfosCache(this.BuildAssetInfos);

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

        [System.Flags]
        public enum BuildMode
        {
            BuildAB = 1 << 1,
            GenArtAssetInfo = 1 << 2,
            ALL = BuildAB | GenArtAssetInfo,
        }

        /// <summary>
        /// 开始构建AB
        /// </summary>
        public void StartBuildAssetBundle(BuildTarget buildTarget, List<string> changedAssetList = null, BuildMode buildMode = BuildMode.ALL)
        {
            //-----------------------开始打包AssetBundle逻辑---------------------------
            Debug.Log("【BuildAssetbundle】执行Build...");
            //设置编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.BuildAssetBundle;
            var platform = BApplication.GetRuntimePlatform(buildTarget);
            var platformOutputPath = IPath.Combine(BuildParams.OutputPath, BApplication.GetPlatformPath(platform));
            string abOutputPath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_ROOT_PATH);
            //打包
            AssetDatabase.StartAssetEditing(); //禁止自动导入
            {
                try
                {

                    //--------------------------------开始打包----------------------------------
                    //1.打包
                    Debug.Log("<color=green>----->1.进入打包逻辑</color>");
                    //整理ab颗粒度
                    BuildAssetInfos.ReorganizeAssetBundleUnit();
                    //获取ab列表
                    this.AssetBundleItemList = BuildAssetInfos.GetAssetBundleItems();
                    //对比差异文件
                    //var changedAssetsInfoList = AssetBundleToolsV2.GetChangedAssetsByFileHash(this.BuildParams.OutputPath, buildTarget, BuildAssetInfos);
                    //------->打包<-------
                    if (buildMode.HasFlag(BuildMode.BuildAB))
                    {
                        this.BuildAssetBundle_SBP(AssetBundleItemList, BuildAssetInfos.AssetInfoMap.ToList(), BuildParams, platform);
                    }
                    //------->打包<-------

                    //2.清理
                    Debug.Log("<color=green>----->2.清理旧ab</color>");
                    //删除本地没有的资源
                    var allABList = Directory.GetFiles(abOutputPath, "*", SearchOption.AllDirectories).Where((p) => string.IsNullOrEmpty(Path.GetExtension(p)));
                    foreach (var abpath in allABList)
                    {
                        var abname = Path.GetFileName(abpath);
                        var ret = AssetBundleItemList.FirstOrDefault((abdata) => abdata.AssetBundlePath == abname);
                        if (ret == null)
                        {
                            File.Delete(abpath);
                            var path = AssetDatabase.GUIDToAssetPath(abname);
                            Debug.Log("【删除旧ab:】" + abname + "  -  " + path);
                        }
                    }


                    //3.BuildInfo配置处理
                    Debug.Log($"<color=green>----->3.BuildInfo相关生成  abcount:{AssetBundleItemList.Count}</color>");
                    var lastAssetInfoPath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_INFO_PATH);
                    List<AssetBundleItem> lastAssetbundleItemList = new List<AssetBundleItem>();
                    if (File.Exists(lastAssetInfoPath))
                    {
                        lastAssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(File.ReadAllText(lastAssetInfoPath));
                    }

                    Dictionary<string, string> abHashCacheMap = new Dictionary<string, string>();
                    //设置ab的hash
                    for (int i = 0; i < AssetBundleItemList.Count; i++)
                    {
                        var newAbi = AssetBundleItemList[i];

                        if (newAbi.IsAssetBundleFile())
                        {
                            var abpath = IPath.Combine(platformOutputPath, BResources.ART_ASSET_ROOT_PATH, newAbi.AssetBundlePath);
                            Debug.Log($"===>获取ABhash:{newAbi.AssetBundlePath}  <color=yellow>[{i}/{AssetBundleItemList.Count - 1}]</color>");
                            //增加缓存，避免频繁获取大ab浪费时间
                            if (!abHashCacheMap.TryGetValue(abpath, out var hash))
                            {
                                hash = FileHelper.GetMurmurHash3(abpath);
                                abHashCacheMap[abpath] = hash;
                            }

                            newAbi.Hash = hash;
                        }
                    }


                    //获取上一次打包的数据，跟这次打包数据合并


                    foreach (var newABI in AssetBundleItemList)
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


                    //判断变动
                    List<AssetBundleItem> changedAssetBundleItems = new List<AssetBundleItem>();
                    for (int i = 0; i < AssetBundleItemList.Count; i++)
                    {
                        var newAbi = AssetBundleItemList[i];
                        if (newAbi.IsAssetBundleFile())
                        {
                            //判断是否变动
                            if (lastAssetbundleItemList != null)
                            {
                                var lastABI = lastAssetbundleItemList.FirstOrDefault((last) => newAbi.AssetBundlePath.Equals(last.AssetBundlePath, StringComparison.OrdinalIgnoreCase)); //AB名相等
                                if (lastABI == null)
                                {
                                    changedAssetBundleItems.Add(newAbi);
                                    Debug.Log($"增加AB:<color=red>{AssetDatabase.GUIDToAssetPath(newAbi.AssetBundlePath)}</color>");
                                }
                                else if (lastABI.Hash != newAbi.Hash)
                                {
                                    changedAssetBundleItems.Add(newAbi);
                                    Debug.Log($"修改AB:<color=red>{AssetDatabase.GUIDToAssetPath(newAbi.AssetBundlePath)}</color>");
                                }
                            }
                        }
                    }

                    Debug.Log($"<color=yellow> 变动AB数量：{changedAssetBundleItems.Count}</color>");

                    #region 保存AssetTypeConfig

                    //保存AssetsType
                    var asetsTypePath = string.Format("{0}/{1}/{2}", this.BuildParams.OutputPath, BApplication.GetPlatformPath(buildTarget), BResources.ART_ASSET_TYPES_PATH);
                    //数据结构保存
                    AssetTypeConfig at = new AssetTypeConfig()
                    {
                        AssetTypeList = this.BuildAssetInfos.AssetTypeList,
                    };
                    var typesCsv = CsvSerializer.SerializeToString(at);
                    FileHelper.WriteAllText(asetsTypePath, typesCsv);
                    Debug.LogFormat("AssetType写入到:{0} \n{1}", asetsTypePath, typesCsv);

                    #endregion

                    //保存artconfig.info
                    var csv = CsvSerializer.SerializeToString(AssetBundleItemList);
                    FileHelper.WriteAllText(lastAssetInfoPath, csv);
                    Debug.Log("写入成功:" + lastAssetInfoPath);

                    //保存BuildInfo配置
                    var editorBuildinfoPath = IPath.Combine(platformOutputPath, BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH);
                    //缓存buildinfo
                    var json = JsonMapper.ToJson(BuildAssetInfos, true);
                    FileHelper.WriteAllText(editorBuildinfoPath, json);
                    Debug.Log("写入成功:" + editorBuildinfoPath);

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

                        if (!File.Exists(manifest))
                        {
                            continue;
                        }

                        var lines = File.ReadLines(manifest);
                        List<string> manifestDependList = new List<string>();
                        bool isReadAssets = false;
                        bool isReadDependcies = false;
                        foreach (var line in lines)
                        {
                            if (!isReadAssets && line.Equals("Assets:"))
                            {
                                isReadAssets = true;
                            }
                            else if (line.Contains("Dependencies:"))
                            {
                                isReadAssets = false;
                                isReadDependcies = true;
                            }
                            else if (isReadAssets)
                            {
                                var tag = "- ";
                                if (line.StartsWith(tag))
                                {
                                    var file = line.Replace("- ", "");
                                    manifestDependList.Add(file.ToLower());
                                }
                                else
                                {
                                    //这一行属于上一行
                                    var idx = manifestDependList.Count - 1;
                                    manifestDependList[idx] = (manifestDependList[idx] + line.Remove(0, 1).ToLower());
                                }
                            }
                            else if (isReadDependcies)
                            {
                            }
                        }

                        //对比包集合
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
                                Debug.LogError($"【AssetbundleV2-验证】本地AssetBundle依赖与预期不符:\n 本地-{manifestDependList.Count}: {local} \n 预期-{previewABDependList.Count}:{preview}");
                                var except2 = previewABDependList.Except(manifestDependList);
                                Debug.LogError($"差异: \n local: {JsonMapper.ToJson(except.ToArray(), true)} \n config:{JsonMapper.ToJson(except2.ToArray(), true)}");
                            }
                        }

                        //对比依赖
                    }


                    //6.资源混淆
                    if (BDEditorApplication.EditorSetting.BuildAssetBundleSetting.IsEnableObfuscation)
                    {
                        Debug.Log("<color=green>----->6.混淆AB</color>");
                        AssetBundleToolsV2.MixAssetBundle(BuildParams.OutputPath, platform);
                    }

                    //The end.最后处理
                    //移除所有的ab
                    //AssetBundleToolsV2.RemoveAllAssetbundleName();
                    //恢复编辑器状态
                    BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.Idle;
                    //BD生命周期触发
                    BDFrameworkPipelineHelper.OnEndBuildAssetBundle(this);


                    //GenAssetBundleItemCacheList = abConfigList.ToList();
                }
                catch (Exception e)
                {
                    AssetDatabase.StopAssetEditing(); //恢复自动导入
                    Console.WriteLine(e);
                    throw;
                }
            }
            
            //更新
            var version = BDFrameworkPipelineHelper.GetArtSVCNum(BuildParams.OutputPath, platform);
            ClientAssetsHelper.GenBasePackageBuildInfo(BuildParams.OutputPath, platform, artSVC: version);
            AssetDatabase.StopAssetEditing(); //恢复自动导入
        }


        /// <summary>
        /// 获取AssetbundleBuildList
        /// </summary>
        /// <returns></returns>
        private List<AssetBundleBuild> GetAssetbundleBuidList(List<KeyValuePair<string, BuildAssetInfos.AssetInfo>> buildAssetInfos)
        {
            //按ab进行排序
            Dictionary<string, List<KeyValuePair<string, BuildAssetInfos.AssetInfo>>> assetinfoDic = new Dictionary<string, List<KeyValuePair<string, BuildAssetInfos.AssetInfo>>>();
            foreach (var item in buildAssetInfos)
            {
                var ab = item.Value.ABName;
                if (!assetinfoDic.ContainsKey(ab))
                {
                    assetinfoDic[ab] = new List<KeyValuePair<string, BuildAssetInfos.AssetInfo>>();
                }

                //添加
                //资产存在，且不是文件夹
                if (File.Exists(item.Key))
                {
                    assetinfoDic[ab].Add(item);
                }
            }

            //构建
            List<AssetBundleBuild> abBuildList = new List<AssetBundleBuild>();
            foreach (var item in assetinfoDic)
            {
                var build = new AssetBundleBuild();
                build.assetBundleName = item.Key;
                //构建
                build.assetNames = new String[item.Value.Count];
                build.addressableNames = new String[item.Value.Count];
                //
                for (int i = 0; i < item.Value.Count; i++)
                {
                    var assetPath = item.Value[i].Key;
                    build.assetNames[i] = assetPath;
                    //GUID作为ab的加载路径
                    build.addressableNames[i] = AssetDatabase.AssetPathToGUID(assetPath);
                }

                abBuildList.Add(build);
            }

            BDebug.Log($"最终AssetBundleBuild数量:{buildAssetInfos.Count}  AB个数:{abBuildList.Count} ", "red");

            return abBuildList;
        }

        /// <summary>
        /// 打包Asset Bundle
        /// </summary>
        /// <param name="buildAssetInfos"></param>
        /// <param name="buildParams"></param>
        /// <param name="platform"></param>
        private void BuildAssetBundle(List<AssetBundleItem> assetBundleItemList, List<KeyValuePair<string, BuildAssetInfos.AssetInfo>> buildAssetInfos, BuildAssetBundleParams buildParams, RuntimePlatform platform)
        {
            //buildlist


            var abBuildList = GetAssetbundleBuidList(buildAssetInfos);

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
            var buildAssetConf = BDEditorApplication.EditorSetting?.BuildAssetBundleSetting;
            if (buildAssetConf.IsDisableTypeTree)
            {
                buildOpa |= BuildAssetBundleOptions.DisableWriteTypeTree; //关闭TypeTree
            }


            UnityEditor.BuildPipeline.BuildAssetBundles(abOutputPath, abBuildList.ToArray(), buildOpa, buildTarget);
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

         
        }


        /// <summary>
        /// SBP模式构建
        /// </summary>
        private void BuildAssetBundle_SBP(List<AssetBundleItem> assetBundleItemList, List<KeyValuePair<string, BuildAssetInfos.AssetInfo>> buildAssetInfos, BuildAssetBundleParams inputParams, RuntimePlatform platform)
        {
            //1.构建buildContent
            var abBuildList = GetAssetbundleBuidList(buildAssetInfos);
            var buildContent = new BundleBuildContent(abBuildList);
            //2.构建参数
            string abOutputPath = IPath.Combine(inputParams.OutputPath, BApplication.GetPlatformPath(platform), BResources.ART_ASSET_ROOT_PATH);
            if (!Directory.Exists(abOutputPath))
            {
                Directory.CreateDirectory(abOutputPath);
            }

            var buildParams = new BundleBuildParameters(BApplication.GetBuildTarget(platform), BApplication.GetBuildTargetGroup(platform), abOutputPath);
            //使用CacheServer，Editor下 CacheServer(specific)
            var address = EditorSettings.cacheServerEndpoint.Split(':');
            var ip = address[0];
            UInt16 port = 0; // If 0, will use the default set port
            if (address.Length == 2)
            {
                port = Convert.ToUInt16(address[1]);
            }

            bool iscanConnectCacheServer = AssetDatabaseExperimental.CanConnectToCacheServer(ip, port);
            //SBP
            if (!iscanConnectCacheServer)
            {
                for (int i = 0; i < 10; i++)
                {
                    UnityEngine.Debug.LogError("建议配置 CacheServer,避免相同资产打包Assetbundle不一致!! (Project Setting/Editor/CacheServer(project specific))");
                }
            }

            buildParams.UseCache = true;
            buildParams.CacheServerHost = ip;
            buildParams.CacheServerPort = port;
            //LZ4压缩
            buildParams.BundleCompression = BuildCompression.LZ4;
            buildParams.AppendHash = false;


            IBundleBuildResults results;
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results);

            Debug.Log("打包结果:" + exitCode.ToString());
            string buildinfoOutputPath = IPath.Combine(inputParams.OutputPath, BApplication.GetPlatformPath(platform), "build_result.info");
            if (results != null)
            {
                File.WriteAllText(buildinfoOutputPath, JsonMapper.ToJson(results.BundleInfos, true));
            }

          
        }

        #endregion
    }
}
