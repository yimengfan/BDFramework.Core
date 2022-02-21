using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using LitJson;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
using Debug = UnityEngine.Debug;
using String = System.String;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[Build]打包AssetBundle", 100)]
    public class BuildAssetBundle : UnityEngine.AssetGraph.Node, IBDFrameowrkAssetEnvParams
    {
        public BuildInfo BuildInfo { get; set; }
        public BuildAssetBundleParams BuildParams { get; set; }

        public void Reset()
        {
        }

        public override string ActiveStyle
        {
            get { return "node 7 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 7"; }
        }

        public override string Category
        {
            get { return "打包AssetBundle"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddOutputPoint("预览打包结果");
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new BuildAssetBundle();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
        }


        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            Debug.Log("【BuildAssetbundle】执行Prepare");
            if (this.BuildParams == null)
            {
                this.BuildParams = BDFrameworkAssetsEnv.BuildParams;
            }

            //这里只做临时的输出，预览用，不做实际更改
            BuildInfo tempBuildInfo = new BuildInfo();
            var json = JsonMapper.ToJson(BDFrameworkAssetsEnv.BuildInfo);
            var temp = JsonMapper.ToObject<BuildInfo>(json);
            foreach (var item in temp.AssetDataMaps)
            {
                tempBuildInfo.AssetDataMaps[item.Key] = item.Value;
            }

            Debug.Log("Buildinfo 数量:" + tempBuildInfo.AssetDataMaps.Count);

            //预计算输出,不直接修改buildinfo
            // var platform = BDApplication.GetRuntimePlatform(target);
            this.MergeABName(tempBuildInfo);

            //对比差异文件
            var changedBuildInfo = GetChangedAssets(tempBuildInfo, target);

            //搜集所有的 asset reference 
            List<AssetReference> assetReferenceList = new List<AssetReference>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    assetReferenceList.AddRange(ag.Value);
                }
            }

            //验证资源
            if (assetReferenceList.Count == BDFrameworkAssetsEnv.BuildInfo.AssetDataMaps.Count)
            {
                foreach (var ar in assetReferenceList)
                {
                    BDFrameworkAssetsEnv.BuildInfo.AssetDataMaps.TryGetValue(ar.importFrom, out var test);
                    if (!BDFrameworkAssetsEnv.BuildInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                    {
                        Debug.LogError("【资源验证】不存在:" + ar.importFrom);
                    }
                }
            }
            else
            {
                var list = new List<string>();
                if (assetReferenceList.Count > tempBuildInfo.AssetDataMaps.Count)
                {
                    foreach (var ar in assetReferenceList)
                    {
                        if (!tempBuildInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                        {
                            list.Add(ar.importFrom);
                        }
                    }

                    Debug.Log("Buildinfo缺少资源:\n " + JsonMapper.ToJson(list));
                }
                else
                {
                    foreach (var key in tempBuildInfo.AssetDataMaps.Keys)
                    {
                        var ret = assetReferenceList.Find((ar) => ar.importFrom.Equals(key, StringComparison.OrdinalIgnoreCase));
                        if (ret == null)
                        {
                            list.Add(key);
                        }
                    }

                    Debug.Log("Buildinfo多余资源:\n " + JsonMapper.ToJson(list, true));
                }

                Debug.LogErrorFormat("【资源验证】coming资源和Buildinfo资源数量不相等!{0}-{1}", assetReferenceList.Count, tempBuildInfo.AssetDataMaps.Count);
            }


            //输出节点 预览
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var buildAssetItem in tempBuildInfo.AssetDataMaps)
            {
                if (!outMap.TryGetValue(buildAssetItem.Value.ABName, out var list))
                {
                    list = new List<AssetReference>();
                    outMap[buildAssetItem.Value.ABName] = list;
                }

                //找到资源的assetref
                var ar = assetReferenceList.Find((a) => a.importFrom.Equals(buildAssetItem.Key, StringComparison.OrdinalIgnoreCase));
                if (ar != null)
                {
                    list.Add(ar);
                }
                else
                {
                    Debug.LogFormat("<color=red>【BuildAssetBundle】资源没有inComing:{0} </color>", buildAssetItem.Key);
                }
            }

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }


        /// <summary>
        /// 生成的assetbundleitem 列表
        /// </summary>
        public static List<AssetBundleItem> GenAssetBundleItemCacheList { get; private set; } = null;

        /// <summary>
        /// buildinfo 缓存列表
        /// </summary>
        public static BuildInfo BuildInfoCache { get; private set; } = null;

        public override void Build(BuildTarget buildTarget, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc,
            Action<NodeData, string, float> progressFunc)
        {
            Debug.Log("【BuildAssetbundle】执行Build...");
            //设置编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.BuildAssetBundle;
            //开始打包逻辑
            if (this.BuildInfo == null)
            {
                this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;
            }

            if (this.BuildParams == null)
            {
                this.BuildParams = BDFrameworkAssetsEnv.BuildParams;
            }

            //-----------------------开始打包AssetBundle逻辑---------------------------
            var platform = BDApplication.GetRuntimePlatform(buildTarget);
            //1.整理abname
            this.MergeABName(BuildInfo);
            //2.对比差异文件
            var changedBuildInfo = GetChangedAssets(BuildInfo, buildTarget);
            //3.生成artconfig
            var abConfigList = this.GenAssetBundleConfig(BuildInfo, BuildParams, platform);

            // foreach (var item in abConfigList)
            // {
            //     if (!item.LoadPath.Contains(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase))
            //     {
            //         
            //     }
            // }

            //4.打包
            AssetDatabase.StartAssetEditing(); //禁止自动导入
            {
                this.BuildAB(abConfigList, changedBuildInfo, BuildParams, platform);
            }
            AssetDatabase.StopAssetEditing(); //恢复自动导入

            //3.BuildInfo配置处理
            var platformOutputPath = String.Format("{0}/{1}", BuildParams.OutputPath, BDApplication.GetPlatformPath(platform));
            //保存artconfig.info
            var configPath = String.Format("{0}/{1}", platformOutputPath, BResources.ASSET_CONFIG_PATH);
            var csv = CsvSerializer.SerializeToString(abConfigList);
            FileHelper.WriteAllText(configPath, csv);

            //II:保存BuildInfo配置
            var buildinfoPath = IPath.Combine(platformOutputPath, BResources.EDITOR_ASSET_BUILD_INFO_PATH);
            // //移动老配置
            // if (File.Exists(buildinfoPath))
            // {
            //     string oldBuildInfoPath = Path.Combine(platformOutputPath, BResources.ASSET_OLD_BUILD_INFO_PATH);
            //     File.Delete(oldBuildInfoPath);
            //     File.Move(buildinfoPath, oldBuildInfoPath);
            // }

            //缓存buildinfo
            var json = JsonMapper.ToJson(BuildInfo, true);
            FileHelper.WriteAllText(buildinfoPath, json);


            //4.备份Artifacts
            //this.BackupArtifacts(buildTarget);

            //恢复编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.Idle;
            //BD生命周期触发
            BDFrameworkPublishPipelineHelper.OnEndBuildAssetBundle(this.BuildParams, this.BuildInfo);


            //缓存
            BuildInfoCache = new BuildInfo();
            var cachejson = JsonMapper.ToJson(BDFrameworkAssetsEnv.BuildInfo);
            var temp = JsonMapper.ToObject<BuildInfo>(cachejson);
            foreach (var item in temp.AssetDataMaps)
            {
                BuildInfoCache.AssetDataMaps[item.Key] = item.Value;
            }

            GenAssetBundleItemCacheList = abConfigList.ToList();


            BDFrameworkAssetsEnv.BuildInfo = null;
        }


        /// <summary>
        /// 打包Asset Bundle
        /// </summary>
        /// <param name="buildInfo"></param>
        /// <param name="buildParams"></param>
        /// <param name="platform"></param>
        private void BuildAB(List<AssetBundleItem> assetBundleItemList, BuildInfo buildInfo, BuildAssetBundleParams buildParams, RuntimePlatform platform)
        {
            //----------------------------开始设置build ab name-------------------------------
            //根据传进来的资源,设置AB name
            foreach (var buildInfoItem in buildInfo.AssetDataMaps)
            {
                var assetPath = buildInfoItem.Key;
                var assetData = buildInfoItem.Value;
                //设置ab name
                var ai = GetAssetImporter(assetPath);
                if (ai)
                {
                    // Debug.Log("【设置AB】" + assetReference.importFrom + " - " + assetData.ABName);
                    //ai.assetBundleName = assetData.ABName;
                    ai.SetAssetBundleNameAndVariant(assetData.ABName, null);
                }
            }


            //----------------------------生成AssetBundle-------------------------------
            var platformOutputPath = Path.Combine(buildParams.OutputPath, BDApplication.GetPlatformPath(platform));
            string abOutputPath = IPath.Combine(platformOutputPath, BResources.ASSET_ROOT_PATH);
            // try
            // {

            if (!Directory.Exists(abOutputPath))
            {
                Directory.CreateDirectory(abOutputPath);
            }

            var buildTarget = BDApplication.GetBuildTarget(platform);
            var buildOpa = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle;
            
            UnityEditor.BuildPipeline.BuildAssetBundles(abOutputPath, buildOpa, buildTarget);
            Debug.LogFormat("【编译AssetBundle】 output:{0} ,buildTarget:{1}", abOutputPath, buildTarget.ToString());
            // }
            // catch (Exception e)
            // {
            //     Debug.LogException(e);
            //     throw;
            // }

            //----------------------------清理-------------------------------------
            //1.移除所有的ab
            RemoveAllAssetbundleName();
            //2.删除本地没有的资源
            var allAbList = Directory.GetFiles(abOutputPath, "*", SearchOption.AllDirectories);
            foreach (var abpath in allAbList)
            {
                if (abpath.Contains("."))
                {
                    continue;
                }

                var abname = Path.GetFileName(abpath);
                var ret = assetBundleItemList.FirstOrDefault((abdata) => abdata.AssetBundlePath == abname);
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

            GameAssetHelper.GenPackageBuildInfo(buildParams.OutputPath, platform);
        }


        #region AB相关处理

        static string RUNTIME_PATH = "/runtime/";

        /// <summary>
        /// 合并ABname
        /// </summary>
        private void MergeABName(BuildInfo buildInfo)
        {
            #region 整理依赖关系

            //1.把依赖资源替换成AB Name，
            foreach (var mainAsset in buildInfo.AssetDataMaps.Values)
            {
                for (int i = 0; i < mainAsset.DependAssetList.Count; i++)
                {
                    var dependAssetName = mainAsset.DependAssetList[i];
                    if (buildInfo.AssetDataMaps.TryGetValue(dependAssetName, out var dependAssetData))
                    {
                        //替换成真正AB名
                        if (!string.IsNullOrEmpty(dependAssetData.ABName))
                        {
                            mainAsset.DependAssetList[i] = dependAssetData.ABName;
                        }
                    }
                    else
                    {
                        Debug.Log("【AssetbundleV2】资源整理出错: " + dependAssetName);
                    }
                }

                //去重，移除自己
                mainAsset.DependAssetList = mainAsset.DependAssetList.Distinct().ToList();
                mainAsset.DependAssetList.Remove(mainAsset.ABName);
            }

            //2.按规则纠正ab名
            //使用guid 作为ab名
            foreach (var mainAsset in buildInfo.AssetDataMaps)
            {
                var guid = AssetDatabase.AssetPathToGUID(mainAsset.Value.ABName);
                if (!string.IsNullOrEmpty(guid)) //不存在的资源（如ab.shader之类）,则用原名
                {
                    mainAsset.Value.ABName = guid;
                }
                else
                {
                    Debug.LogError("获取GUID失败：" + mainAsset.Value.ABName);
                }

                for (int i = 0; i < mainAsset.Value.DependAssetList.Count; i++)
                {
                    var dependAssetPath = mainAsset.Value.DependAssetList[i];

                    guid = AssetDatabase.AssetPathToGUID(dependAssetPath);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        mainAsset.Value.DependAssetList[i] = guid;
                    }
                    else
                    {
                        Debug.LogError("获取GUID失败：" + dependAssetPath);
                    }
                }
            }

            #endregion
        }

        /// <summary>
        ///生成Runtime下的Art.Config
        /// </summary>
        private List<AssetBundleItem> GenAssetBundleConfig(BuildInfo buildInfo, BuildAssetBundleParams buildParams, RuntimePlatform platform)
        {
            //根据buildinfo 生成加载用的 Config
            //runtime下的全部保存配置，其他的只保留一个ab名即可
            //1.导出配置
            var assetDataItemList = new List<AssetBundleItem>();
            //占位，让id和idx恒相等
            assetDataItemList.Add(new AssetBundleItem(0, null, null, -1, new List<int>()));
         
            //搜集runtime的 ,分两个for 让序列化后的数据更好审查
            foreach (var item in buildInfo.AssetDataMaps)
            {
                //runtime路径下，写入配置
                if (item.Key.Contains(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase))
                {
                    var loadPath = GetAbsPathFormRuntime(item.Key);
                    //添加
                    var abi = new AssetBundleItem(assetDataItemList.Count, loadPath, item.Value.ABName, item.Value.Type, new List<int>());
                    // abi.EditorAssetPath = item.Key;
                    assetDataItemList.Add(abi); //.ManifestMap[key] = mi;
                    item.Value.ArtConfigIdx = abi.Id;
                }
            }
            
            //搜集非runtime的,进一步防止重复
            foreach (var item in buildInfo.AssetDataMaps)
            {
                //非runtime的只需要被索引AB
                if (!item.Key.Contains(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase))
                {
                    var ret = assetDataItemList.FirstOrDefault((ab) => ab.AssetBundlePath == item.Value.ABName);
                    if (ret == null) //不保存重复内容
                    {
                        var abi = new AssetBundleItem(assetDataItemList.Count, null, item.Value.ABName, item.Value.Type, new List<int>());
                        // abi.EditorAssetPath = item.Key;
                        assetDataItemList.Add(abi);
                        item.Value.ArtConfigIdx = abi.Id;
                    }
                }
            }



            //将depend替换成Id,减少序列化数据量
            foreach (var assetbundleData in assetDataItemList)
            {
                if (!string.IsNullOrEmpty(assetbundleData.LoadPath))
                {
                    //dependAsset 替换成ID
                    var buildAssetData = buildInfo.AssetDataMaps.Values.FirstOrDefault((asset) => asset.ABName == assetbundleData.AssetBundlePath);
                    for (int i = 0; i < buildAssetData.DependAssetList.Count; i++)
                    {
                        var dependAssetName = buildAssetData.DependAssetList[i];
                        //寻找保存列表中依赖的id（可以认为是下标）
                        var dependAssetBuildData = assetDataItemList.FirstOrDefault((asset) => asset.AssetBundlePath == dependAssetName);
                        assetbundleData.DependAssetIds.Add(dependAssetBuildData.Id);
                    }
                }
            }


            #region 检查生成的数据

            //检查config是否遗漏
            foreach (var assetDataItem in buildInfo.AssetDataMaps)
            {
                var ret = assetDataItemList.Find((abi) => abi.AssetBundlePath == assetDataItem.Value.ABName);
                if (ret == null)
                {
                    Debug.LogError("【生成配置】ab配置遗漏 - " + assetDataItem.Key + " ab:" + assetDataItem.Value.ABName);
                }
            }

            #endregion


            //
            return assetDataItemList;
        }

        /// <summary>
        /// 获取runtime后的相对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string GetAbsPathFormRuntime(string loadPath)
        {
            //移除runtime之前的路径、后缀
            if (loadPath.Contains(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase))
            {
                var index = loadPath.IndexOf(RUNTIME_PATH);
                loadPath = loadPath.Substring(index + RUNTIME_PATH.Length); //hash要去掉runtime
                var exten = Path.GetExtension(loadPath);
                if (!string.IsNullOrEmpty(exten))
                {
                    loadPath = loadPath.Replace(exten, "");
                }
            }

            return loadPath;
        }

        #endregion


        #region 资源变动查询

        /// <summary>
        /// 获取改动的Assets
        /// </summary>
        BuildInfo GetChangedAssets(BuildInfo newBuildInfo, BuildTarget buildTarget)
        {
            Debug.Log("<color=red>【增量资源】开始变动资源分析...</color>");
            BuildInfo lastBuildInfo = null;
            var configPath = this.BuildParams.OutputPath + "/" + BDApplication.GetPlatformPath(buildTarget) + "/" + BResources.EDITOR_ASSET_BUILD_INFO_PATH;
            Debug.Log("旧资源地址:" + configPath);
            if (File.Exists(configPath))
            {
                var configContent = File.ReadAllText(configPath);
                lastBuildInfo = JsonMapper.ToObject<BuildInfo>(configContent);
            }


            //根据变动的list 刷出关联
            //I.单ab 单资源，直接重打
            //II.单ab 多资源的 整个ab都要重新打包
            if (lastBuildInfo != null && lastBuildInfo.AssetDataMaps.Count != 0)
            {
                #region 文件改动

                var changedAssetList = new List<KeyValuePair<string, BuildInfo.BuildAssetData>>();
                var changedAssetNameList = new List<string>();
                //1.找出差异文件
                foreach (var newAssetItem in newBuildInfo.AssetDataMaps)
                {
                    if (lastBuildInfo.AssetDataMaps.TryGetValue(newAssetItem.Key, out var lastAssetItem))
                    {
                        //文件修改打包
                        if (lastAssetItem.Hash == newAssetItem.Value.Hash)
                        {
                            continue;
                        }
                    }

                    changedAssetList.Add(newAssetItem);
                }

                Debug.LogFormat("<color=red>【增量资源】变动文件数:{0}</color>", changedAssetList.Count);
                var changedContentFiles = new List<string>();
                foreach (var item in changedAssetList)
                {
                    changedContentFiles.Add(item.Key);
                }

                Debug.Log(JsonMapper.ToJson(changedContentFiles, true));

                #endregion

                #region ABName修改 （abName修改会导致引用该ab的所有资源重新构建 才能保证正常引用关系 上线项目尽量不要有ab修改的情况）

                var changedABNameAssetList = new List<KeyValuePair<string, BuildInfo.BuildAssetData>>();
                //1.找出AB修改的文件
                foreach (var newAssetItem in newBuildInfo.AssetDataMaps)
                {
                    if (lastBuildInfo.AssetDataMaps.TryGetValue(newAssetItem.Key, out var lastAssetItem))
                    {
                        //ABName 修改打包
                        if (lastAssetItem.ABName != newAssetItem.Value.ABName)
                        {
                            changedABNameAssetList.Add(newAssetItem);
                        }
                    }
                }

                Debug.LogFormat("<color=red>【增量资源】修改ABName文件数:{0}</color>", changedABNameAssetList.Count);
                var changeABNameFiles = new List<string>();
                foreach (var item in changedABNameAssetList)
                {
                    changeABNameFiles.Add(item.Key);
                }

                Debug.Log(JsonMapper.ToJson(changeABNameFiles, true));
                //引用该资源的也要重打,以保证AB正确的引用关系
                var changedCount = changedABNameAssetList.Count;
                for (int i = 0; i < changedCount; i++)
                {
                    var asset = changedABNameAssetList[i];
                    var abname = asset.Value.ABName;
                    foreach (var item in newBuildInfo.AssetDataMaps)
                    {
                        if (item.Value.DependAssetList.Contains(abname))
                        {
                            changedABNameAssetList.Add(item);
                        }
                    }
                }

                changedABNameAssetList = changedABNameAssetList.Distinct().ToList();
                //log
                Debug.LogFormat("<color=red>【增量资源】修改ABName影响文件数:{0}，{1}</color>", changedABNameAssetList.Count, "线上项目不建议修改abname！！！");
                changeABNameFiles = new List<string>();
                foreach (var item in changedABNameAssetList)
                {
                    changeABNameFiles.Add(item.Key);
                }

                Debug.Log(JsonMapper.ToJson(changeABNameFiles, true));

                #endregion

                //合并
                changedAssetList.AddRange(changedABNameAssetList);

                //2.依赖资源也要重新打，不然会在这次导出过程中unity默认会把依赖和该资源打到一个ab中
                foreach (var changedAsset in changedAssetList)
                {
                    //1.添加自身的ab
                    changedAssetNameList.Add(changedAsset.Value.ABName);
                    //2.添加所有依赖的ab
                    changedAssetNameList.AddRange(changedAsset.Value.DependAssetList);
                }

                changedAssetNameList = changedAssetNameList.Distinct().ToList();


                //3.搜索相同的ab name的资源,都要重新打包
                var count = changedAssetNameList.Count;
                for (int i = 0; i < count; i++)
                {
                    var rebuildABName = changedAssetNameList[i];
                    var theSameABNameAssets = newBuildInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == rebuildABName);
                    if (theSameABNameAssets != null)
                    {
                        foreach (var mainAssetItem in theSameABNameAssets)
                        {
                            //添加资源本体
                            changedAssetNameList.Add(mainAssetItem.Value.ABName);
                            //添加影响的依赖文件
                            changedAssetNameList.AddRange(mainAssetItem.Value.DependAssetList);
                        }
                    }
                }

                changedAssetNameList = changedAssetNameList.Distinct().ToList();
                //4.根据影响的ab，寻找出所有文件
                var allRebuildAssets = new List<KeyValuePair<string, BuildInfo.BuildAssetData>>();
                foreach (var abname in changedAssetNameList)
                {
                    var findAssets = newBuildInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == abname);
                    allRebuildAssets.AddRange(findAssets);
                }


                //去重
                var changedBuildInfo = new BuildInfo();
                foreach (var kv in allRebuildAssets)
                {
                    changedBuildInfo.AssetDataMaps[kv.Key] = kv.Value;
                }

                Debug.LogFormat("<color=red>【增量资源】总重打资源数:{0}</color>", changedBuildInfo.AssetDataMaps.Count);
                var changedFiles = new List<string>();
                foreach (var item in changedBuildInfo.AssetDataMaps)
                {
                    changedFiles.Add(item.Key);
                }

                Debug.Log(JsonMapper.ToJson(changedFiles, true));
                return changedBuildInfo;
            }
            else
            {
                Debug.Log("【增量资源】本地无资源，全部重打!");
            }

            return newBuildInfo;
        }

        #endregion


        #region asset缓存、辅助等

        /// <summary>
        /// 资源导入缓存
        /// </summary>
        static private Dictionary<string, AssetImporter> AssetImpoterCacheMap = new Dictionary<string, AssetImporter>();

        /// <summary>
        /// 获取assetimpoter
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static private AssetImporter GetAssetImporter(string path)
        {
            AssetImporter ai = null;
            if (!AssetImpoterCacheMap.TryGetValue(path, out ai))
            {
                ai = AssetImporter.GetAtPath(path);
                AssetImpoterCacheMap[path] = ai;
                if (!ai)
                {
                    Debug.LogError("【打包】获取资源失败:" + path);
                }
            }

            return ai;
        }

        /// <summary>
        /// 移除无效资源
        /// </summary>
        public static void RemoveAllAssetbundleName()
        {
            EditorUtility.DisplayProgressBar("资源清理", "清理中...", 1);
            foreach (var ai in AssetImpoterCacheMap)
            {
                if (ai.Value != null)
                {
                    if (!string.IsNullOrEmpty(ai.Value.assetBundleVariant))
                    {
                        ai.Value.assetBundleVariant = null;
                    }

                    ai.Value.assetBundleName = null;
                }
            }

            AssetImpoterCacheMap.Clear();
            EditorUtility.ClearProgressBar();
        }


        /// <summary>
        /// 备份Artifacts
        /// 以免丢失导致后续打包不一致
        /// </summary>
        public void BackupArtifacts(BuildTarget platform)
        {
            var sourceDir = BDApplication.Library + "/Artifacts";
            var targetDir = string.Format("{0}/{1}/Artifacts", BDApplication.DevOpsPublishAssetsPath, BDApplication.GetPlatformPath(platform));
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            Directory.CreateDirectory(targetDir);
            //复制整个目录
            FileHelper.CopyAllFolderFiles(sourceDir, targetDir);
        }

        #endregion
    }
}
