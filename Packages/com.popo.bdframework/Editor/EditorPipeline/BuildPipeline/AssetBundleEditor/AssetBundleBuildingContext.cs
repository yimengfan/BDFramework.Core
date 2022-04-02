using BDFramework.Core.Tools;
using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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


namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// Assetbundle构建时候的上下文信息
    /// </summary>
    public class AssetBundleBuildingContext
    {
        static public string RUNTIME_PATH = "/runtime/";

        /// <summary>
        /// 构建参数
        /// </summary>
        public BuildAssetBundleParams BuildParams { get; set; } = new BuildAssetBundleParams();

        /// <summary>
        /// 构建资产的数据
        /// </summary>
        public BuildAssetsInfo BuildAssetsInfo { get; private set; } = new BuildAssetsInfo();

        /// <summary>
        /// 所有资产的类型
        /// </summary>
        public List<string> AssetTypeList { get; private set; } = new List<string>();

        /// <summary>
        /// runtime下的资源
        /// </summary>
        public List<AssetReference> RuntimeAssetsList { get; private set; } = new List<AssetReference>();

        /// <summary>
        /// Runtime的依赖资源
        /// </summary>
        public List<AssetReference> DependAssetList { get; private set; } = new List<AssetReference>();

        /// <summary>
        /// 文件hash缓存
        /// </summary>
        Dictionary<string, string> fileHashCacheMap = new Dictionary<string, string>();

        /// <summary>
        /// 依赖列表
        /// </summary>
        Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();


        #region 获取可打包资源信息

        /// <summary>
        /// 生成BuildInfo信息
        /// </summary>
        public bool GenBuildInfo()
        {
            //初始化数据
            this.AssetTypeList = new List<string>();
            this.BuildAssetsInfo = new BuildAssetsInfo();
            this.RuntimeAssetsList = GetRuntimeAssetsInfo();

            //
            var sw = new Stopwatch();
            sw.Start();

            BuildAssetsInfo.Time = DateTime.Now.ToShortDateString();
            int id = 0;

            //搜集所有的依赖
            foreach (var mainAsset in this.RuntimeAssetsList)
            {
                //这里会包含主资源
                var dependAssetPathList = GetDependAssetList(mainAsset.importFrom);

                //获取依赖信息 并加入buildinfo
                foreach (var dependPath in dependAssetPathList)
                {
                    //防止重复
                    if (BuildAssetsInfo.AssetDataMaps.ContainsKey(dependPath))
                    {
                        continue;
                    }

                    //判断资源类型
                    var type = AssetBundleEditorToolsV2.GetMainAssetTypeAtPath(dependPath);
                    if (type == null)
                    {
                        Debug.LogError("获取资源类型失败:" + dependPath);
                        continue;
                    }

                    //构建资源类型
                    var assetData = new BuildAssetsInfo.BuildAssetData();
                    assetData.Id = id;
                    assetData.Hash = this.GetHashFromAssets(dependPath);
                    assetData.ABName = dependPath;
                    var idx = AssetTypeList.FindIndex((a) => a == type.FullName);
                    if (idx == -1)
                    {
                        AssetTypeList.Add(type.FullName);
                        idx = AssetTypeList.Count - 1;
                    }

                    assetData.Type = idx;
                    //获取依赖
                    var dependeAssetList = this.GetDependAssetList(dependPath);
                    assetData.DependAssetList.AddRange(dependeAssetList);
                    //添加
                    BuildAssetsInfo.AssetDataMaps[dependPath] = assetData;
                    id++;
                }
            }

            //TODO AB依赖关系纠正
            /// 已知Unity,bug/设计缺陷：
            ///   1.依赖接口，中会携带自己
            ///   2.如若a.png、b.png 依赖 c.atlas，则abc依赖都会是:a.png 、b.png 、 a.atlas
            foreach (var asset in BuildAssetsInfo.AssetDataMaps)
            {
                //依赖中不包含自己
                asset.Value.DependAssetList.Remove(asset.Value.ABName);
            }


            //获取依赖
            this.DependAssetList = this.GetDependAssetsinfo();
            //---------------------------------------end---------------------------------------------------------

            //检查
            foreach (var ar in this.RuntimeAssetsList)
            {
                if (!BuildAssetsInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                {
                    Debug.LogError("AssetDataMaps遗漏资源:" + ar.importFrom);
                }
            }

            Debug.LogFormat("【GenBuildInfo】耗时:{0}ms.", sw.ElapsedMilliseconds);
            //检测构造的数据
            var count = this.RuntimeAssetsList.Count + this.DependAssetList.Count;
            if (BuildAssetsInfo.AssetDataMaps.Count != count)
            {
                Debug.LogErrorFormat("【初始化框架资源环境】出错! buildinfo:{0} output:{1}", BuildAssetsInfo.AssetDataMaps.Count, count);

                var tmpBuildAssetsInfo = BuildAssetsInfo.Clone();
                foreach (var ra in this.RuntimeAssetsList)
                {
                    tmpBuildAssetsInfo.AssetDataMaps.Remove(ra.importFrom);
                }

                foreach (var drf in this.DependAssetList)
                {
                    tmpBuildAssetsInfo.AssetDataMaps.Remove(drf.importFrom);
                }

                Debug.Log(JsonMapper.ToJson(tmpBuildAssetsInfo.AssetDataMaps, true));

                return false;
            }

            return true;
        }


        /// <summary>
        /// 加载runtime的Asset信息
        /// </summary>
        /// <returns></returns>
        public List<AssetReference> GetRuntimeAssetsInfo()
        {
            var allRuntimeDirects = BDApplication.GetAllRuntimeDirects();
            var assetPathList = new List<string>();
            var retAssetList = new List<AssetReference>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var runtimePath in allRuntimeDirects)
            {
                //创建
                var runtimeGuids = AssetDatabase.FindAssets("", new string[] {runtimePath});
                assetPathList.AddRange(runtimeGuids);
            }

            //去重
            assetPathList = assetPathList.Distinct().Select((guid) => AssetDatabase.GUIDToAssetPath(guid)).ToList();
            assetPathList = CheckAssetsPath(assetPathList.ToArray());
            //
            foreach (var path in assetPathList)
            {
                //var path = AssetDatabase.GUIDToAssetPath(guid);
                //无法获取类型资源，移除
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null)
                {
                    Debug.LogError("【Loder】无法获取资源类型:" + path);
                    continue;
                }

                var outAR = AssetReference.CreateReference(path);
                //添加输出
                retAssetList.Add(outAR);
            }

            Debug.LogFormat("LoadAllRuntimeAssets耗时:{0}ms", sw.ElapsedMilliseconds);
            return retAssetList;
        }

        /// <summary>
        /// 获取依赖资源的info
        /// </summary>
        /// <returns></returns>
        public List<AssetReference> GetDependAssetsinfo()
        {
            //依赖的资源
            var dependAssetList = new List<AssetReference>();
            foreach (var assetDataItem in BuildAssetsInfo.AssetDataMaps)
            {
                //不包含在runtime资源里面
                var ret = this.RuntimeAssetsList.Find((ra) => ra.importFrom.Equals(assetDataItem.Key, StringComparison.OrdinalIgnoreCase));
                if (ret == null)
                {
                    var arf = AssetReference.CreateReference(assetDataItem.Key);
                    dependAssetList.Add(arf);
                }
            }

            return dependAssetList;
        }

        #endregion


        #region 打包

        /// <summary>
        /// 开始构建AB
        /// </summary>
        public void StartBuildAssetBundle(BuildTarget buildTarget)
        {
            //-----------------------开始打包AssetBundle逻辑---------------------------
            Debug.Log("【BuildAssetbundle】执行Build...");
            //设置编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.BuildAssetBundle;

            var platform = BDApplication.GetRuntimePlatform(buildTarget);
            //1.整理abname
            this.MergeABName(BuildAssetsInfo);
            //2.对比差异文件
            var changedBuildInfo = GetChangedAssets(BuildAssetsInfo, buildTarget);
            //3.生成artconfig
            var abConfigList = this.GenAssetBundleConfig(BuildAssetsInfo, BuildParams, platform);
            //4.打包
            AssetDatabase.StartAssetEditing(); //禁止自动导入
            {
                this.BuildAB(abConfigList, changedBuildInfo, BuildParams, platform);
            }
            AssetDatabase.StopAssetEditing(); //恢复自动导入

            //3.BuildInfo配置处理
            var platformOutputPath = IPath.Combine(BuildParams.OutputPath, BDApplication.GetPlatformPath(platform));
            //保存artconfig.info
            var configPath = IPath.Combine(platformOutputPath, BResources.ASSET_CONFIG_PATH);
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
            var json = JsonMapper.ToJson(BuildAssetsInfo, true);
            FileHelper.WriteAllText(buildinfoPath, json);


            //4.备份Artifacts
            //this.BackupArtifacts(buildTarget);

            //5.检测别的本地的Manifest和构建预期对比
            var abRootPath = IPath.Combine(BuildParams.OutputPath, BDApplication.GetPlatformPath(platform), BResources.ASSET_ROOT_PATH);
            var previewABUnitMap = BuildAssetsInfo.PreviewAssetbundleUnit();
            var manifestList = Directory.GetFiles(abRootPath, "*.manifest", SearchOption.AllDirectories);
            //解析 manifest
            foreach (var manifest in manifestList)
            {
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
                    else if (isStartRead)
                    {
                        var file = line.Replace("- ", "");
                        manifestDependList.Add(file);
                    }
                    else if (line.Contains("Dependencies:"))
                    {
                        break;
                    }
                }

                //对比依赖
                var abname = Path.GetFileNameWithoutExtension(manifest);
                previewABUnitMap.TryGetValue(abname, out var previewABDependList);
                bool isEquals = true;
                if (manifestDependList.Count == previewABDependList.Count)
                {
                    foreach (var guid in previewABDependList)
                    {
                        var asset = AssetDatabase.GUIDToAssetPath(guid);

                        if (!manifestList.Contains(asset))
                        {
                            isEquals = false;
                            break;
                        }
                    }
                }
                else
                {
                    isEquals = false;
                }

                if (!isEquals)
                {
                    var local = JsonMapper.ToJson(manifestDependList);
                    var preview = JsonMapper.ToJson(previewABDependList);
                    Debug.LogError($"【AssetbundleV2】本地AssetBundle依赖与预期不符:\n 本地:{local} \n 预期:{preview}");
                }
            }


            //恢复编辑器状态
            BDEditorApplication.EditorStatus = BDFrameworkEditorStatus.Idle;
            //BD生命周期触发
            BDFrameworkPublishPipelineHelper.OnEndBuildAssetBundle(this);


            //GenAssetBundleItemCacheList = abConfigList.ToList();
        }


        /// <summary>
        /// 打包Asset Bundle
        /// </summary>
        /// <param name="buildAssetsInfo"></param>
        /// <param name="buildParams"></param>
        /// <param name="platform"></param>
        private void BuildAB(List<AssetBundleItem> assetBundleItemList, BuildAssetsInfo buildAssetsInfo, BuildAssetBundleParams buildParams, RuntimePlatform platform)
        {
            //----------------------------开始设置build ab name-------------------------------
            //根据传进来的资源,设置AB name
            foreach (var buildInfoItem in buildAssetsInfo.AssetDataMaps)
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
            if (!Directory.Exists(abOutputPath))
            {
                Directory.CreateDirectory(abOutputPath);
            }

            var buildTarget = BDApplication.GetBuildTarget(platform);
            var buildOpa = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle;

            UnityEditor.BuildPipeline.BuildAssetBundles(abOutputPath, buildOpa, buildTarget);
            Debug.LogFormat("【编译AssetBundle】 output:{0} ,buildTarget:{1}", abOutputPath, buildTarget.ToString());


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

        /// <summary>
        /// 合并ABname
        /// </summary>
        public void MergeABName(BuildAssetsInfo buildAssetsInfo)
        {
            #region 整理依赖关系

            //1.把依赖资源替换成AB Name，
            foreach (var mainAsset in buildAssetsInfo.AssetDataMaps.Values)
            {
                for (int i = 0; i < mainAsset.DependAssetList.Count; i++)
                {
                    var dependAssetName = mainAsset.DependAssetList[i];
                    if (buildAssetsInfo.AssetDataMaps.TryGetValue(dependAssetName, out var dependAssetData))
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
            foreach (var mainAsset in buildAssetsInfo.AssetDataMaps)
            {
                var guid = AssetDatabase.AssetPathToGUID(mainAsset.Value.ABName);
                if (!string.IsNullOrEmpty(guid))
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
        private List<AssetBundleItem> GenAssetBundleConfig(BuildAssetsInfo buildAssetsInfo, BuildAssetBundleParams buildParams, RuntimePlatform platform)
        {
            //根据buildinfo 生成加载用的 Config
            //runtime下的全部保存配置，其他的只保留一个ab名即可
            //1.导出配置
            var assetDataItemList = new List<AssetBundleItem>();
            //占位，让id和idx恒相等
            assetDataItemList.Add(new AssetBundleItem(0, null, null, -1, new int[] { }));

            //搜集runtime的 ,分两个for 让序列化后的数据更好审查
            foreach (var item in buildAssetsInfo.AssetDataMaps)
            {
                //runtime路径下，写入配置
                if (item.Key.Contains(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase))
                {
                    var loadPath = GetAbsPathFormRuntime(item.Key);
                    //添加
                    var abi = new AssetBundleItem(assetDataItemList.Count, loadPath, item.Value.ABName, item.Value.Type, new int[] { });
                    // abi.EditorAssetPath = item.Key;
                    assetDataItemList.Add(abi); //.ManifestMap[key] = mi;
                    item.Value.ArtConfigIdx = abi.Id;
                }
            }

            //搜集非runtime的,进一步防止重复
            foreach (var item in buildAssetsInfo.AssetDataMaps)
            {
                //非runtime的只需要被索引AB
                if (!item.Key.Contains(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase))
                {
                    var ret = assetDataItemList.FirstOrDefault((ab) => ab.AssetBundlePath == item.Value.ABName);
                    if (ret == null) //不保存重复内容
                    {
                        var abi = new AssetBundleItem(assetDataItemList.Count, null, item.Value.ABName, item.Value.Type, new int[] { });
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
                    var buildAssetData = buildAssetsInfo.AssetDataMaps.Values.FirstOrDefault((asset) => asset.ABName == assetbundleData.AssetBundlePath);
                    for (int i = 0; i < buildAssetData.DependAssetList.Count; i++)
                    {
                        var dependAssetName = buildAssetData.DependAssetList[i];
                        //寻找保存列表中依赖的id（可以认为是下标）
                        var dependAssetBuildData = assetDataItemList.FirstOrDefault((asset) => asset.AssetBundlePath == dependAssetName);
                        //向array添加元素，editor 略冗余，目的是为了保护 runtime 数据源readonly
                        var templist = assetbundleData.DependAssetIds.ToList();
                        templist.Add(dependAssetBuildData.Id);
                        assetbundleData.DependAssetIds = templist.ToArray();
                    }
                }
            }


            #region 检查生成的数据

            //检查config是否遗漏
            foreach (var assetDataItem in buildAssetsInfo.AssetDataMaps)
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
        public BuildAssetsInfo GetChangedAssets(BuildAssetsInfo newBuildAssetsInfo, BuildTarget buildTarget)
        {
            Debug.Log("<color=red>【增量资源】开始变动资源分析...</color>");
            BuildAssetsInfo lastBuildAssetsInfo = null;
            var buildinfoPath = IPath.Combine(this.BuildParams.OutputPath, BDApplication.GetPlatformPath(buildTarget), BResources.EDITOR_ASSET_BUILD_INFO_PATH);
            Debug.Log("旧资源地址:" + buildinfoPath);
            if (File.Exists(buildinfoPath))
            {
                var configContent = File.ReadAllText(buildinfoPath);
                lastBuildAssetsInfo = JsonMapper.ToObject<BuildAssetsInfo>(configContent);
            }


            //根据变动的list 刷出关联
            //I.单ab 单资源，直接重打
            //II.单ab 多资源的 整个ab都要重新打包
            if (lastBuildAssetsInfo != null && lastBuildAssetsInfo.AssetDataMaps.Count != 0)
            {
                #region 文件改动

                var changedAssetList = new List<KeyValuePair<string, BuildAssetsInfo.BuildAssetData>>();
                var changedAssetNameList = new List<string>();
                //1.找出差异文件：不一致  或者没有
                foreach (var newAssetItem in newBuildAssetsInfo.AssetDataMaps)
                {
                    if (lastBuildAssetsInfo.AssetDataMaps.TryGetValue(newAssetItem.Key, out var lastAssetItem))
                    {
                        //文件hash相同
                        if (lastAssetItem.Hash == newAssetItem.Value.Hash)
                        {
                            //依赖完全相同
                            if (lastAssetItem.DependAssetList.Count == newAssetItem.Value.DependAssetList.Count &&
                                lastAssetItem.DependAssetList.All(newAssetItem.Value.DependAssetList.Contains))
                            {
                                continue;
                            }
                        }
                    }

                    changedAssetList.Add(newAssetItem);
                }

                Debug.LogFormat("<color=red>【增量资源】变动文件数:{0}</color>", changedAssetList.Count);
                var changedAssetContentFiles = new List<string>();
                foreach (var item in changedAssetList)
                {
                    changedAssetContentFiles.Add(item.Key + " - " + item.Value.Hash);
                }

                Debug.Log(JsonMapper.ToJson(changedAssetContentFiles, true));

                #endregion

                #region ABName修改、颗粒度修改

                //abName修改会导致引用该ab的所有资源重新构建 才能保证正常引用关系 上线项目尽量不要有ab修改的情况
                var changedAssetBundleAssetList = new List<KeyValuePair<string, BuildAssetsInfo.BuildAssetData>>();
                //AB颗粒度
                var lastABUnitMap = lastBuildAssetsInfo.PreviewAssetbundleUnit();
                var newABUnitMap = newBuildAssetsInfo.PreviewAssetbundleUnit();
                //遍历处理
                foreach (var newAssetItem in newBuildAssetsInfo.AssetDataMaps)
                {
                    if (lastBuildAssetsInfo.AssetDataMaps.TryGetValue(newAssetItem.Key, out var lastAssetItem))
                    {
                        //AB名修改
                        if (lastAssetItem.ABName != newAssetItem.Value.ABName)
                        {
                            changedAssetBundleAssetList.Add(newAssetItem);
                        }
                        //AB 颗粒度修改
                        else
                        {
                            var lastABUnit = lastABUnitMap[lastAssetItem.ABName];
                            var newABUnit = newABUnitMap[newAssetItem.Value.ABName];
                            //颗粒度修改
                            if (lastABUnit.Count != newABUnit.Count || !lastABUnit.All(newABUnit.Contains))
                            {
                                changedAssetBundleAssetList.Add(newAssetItem);
                            }
                        }
                    }
                }


                Debug.LogFormat("<color=red>【增量资源】修改ABName(颗粒度) 文件数:{0}</color>", changedAssetBundleAssetList.Count);
                var changeABNameFiles = new List<string>();
                foreach (var item in changedAssetBundleAssetList)
                {
                    changeABNameFiles.Add(item.Key);
                }

                Debug.Log(JsonMapper.ToJson(changeABNameFiles, true));
                //引用该资源的也要重打,以保证AB正确的引用关系
                var changedCount = changedAssetBundleAssetList.Count;
                for (int i = 0; i < changedCount; i++)
                {
                    var asset = changedAssetBundleAssetList[i];
                    var abname = asset.Value.ABName;
                    foreach (var item in newBuildAssetsInfo.AssetDataMaps)
                    {
                        if (item.Value.DependAssetList.Contains(abname))
                        {
                            changedAssetBundleAssetList.Add(item);
                        }
                    }
                }

                changedAssetBundleAssetList = changedAssetBundleAssetList.Distinct().ToList();
                //log
                Debug.LogFormat("<color=red>【增量资源】修改ABName(颗粒度)影响文件数:{0}，{1}</color>", changedAssetBundleAssetList.Count, "线上修改谨慎!");
                changeABNameFiles = new List<string>();
                foreach (var item in changedAssetBundleAssetList)
                {
                    changeABNameFiles.Add(item.Key);
                }

                #endregion

                //合并
                changedAssetList.AddRange(changedAssetBundleAssetList);

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
                    var theSameABNameAssets = newBuildAssetsInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == rebuildABName);
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
                var allRebuildAssets = new List<KeyValuePair<string, BuildAssetsInfo.BuildAssetData>>();
                foreach (var abname in changedAssetNameList)
                {
                    var findAssets = newBuildAssetsInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == abname);
                    allRebuildAssets.AddRange(findAssets);
                }


                //去重
                var changedBuildInfo = new BuildAssetsInfo();
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

            return newBuildAssetsInfo;
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

        #endregion


        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string[] GetDependAssetList(string path)
        {
            //全部小写
            //path = path.ToLower();
            List<string> dependList = null;
            if (!DependenciesMap.TryGetValue(path, out dependList))
            {
                dependList = AssetDatabase.GetDependencies(path).Select((s) => s.ToLower()).ToList();
                //检测依赖路径
                dependList = CheckAssetsPath(dependList.ToArray());
                DependenciesMap[path] = dependList;
            }

            return dependList.ToArray();
        }


        /// <summary>
        /// 资源验证
        /// </summary>
        /// <param name="allDependObjectPaths"></param>
        static public List<string> CheckAssetsPath(params string[] assetPathArray)
        {
            var retList = new List<string>(assetPathArray);

            for (int i = assetPathArray.Length - 1; i >= 0; i--)
            {
                var path = assetPathArray[i];

                // //文件夹移除
                // if (AssetDatabase.IsValidFolder(path))
                // {
                //     Debug.Log("【依赖验证】移除目录资产" + path);
                //     assetPathList.RemoveAt(i);
                //     continue;
                // }

                //特殊后缀
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    retList.RemoveAt(i);
                    continue;
                }

                //文件不存在,或者是个文件夹移除
                if (!File.Exists(path) || Directory.Exists(path))
                {
                    retList.RemoveAt(i);
                    continue;
                }

                //判断路径是否为editor依赖
                if (path.Contains("/editor/", StringComparison.OrdinalIgnoreCase) //一般的编辑器资源
                    || path.Contains("/Editor Resources/", StringComparison.OrdinalIgnoreCase) //text mesh pro的编辑器资源
                   )
                {
                    retList.RemoveAt(i);
                    Debug.LogWarning("【依赖验证】移除Editor资源" + path);
                    continue;
                }
            }

            return retList;
        }


        /// <summary>
        /// 获取文件的md5
        /// 同时用资产+资产meta 取 sha256
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetHashFromAssets(string fileName)
        {
            var str = "";
            if (fileHashCacheMap.TryGetValue(fileName, out str))
            {
                return str;
            }

            try
            {
                //这里使用 asset + meta 生成hash,防止其中一个修改导致的文件变动 没更新
                var assetBytes = File.ReadAllBytes(fileName);
                var metaBytes = File.ReadAllBytes(fileName + ".meta");
                List<byte> byteList = new List<byte>();
                byteList.AddRange(assetBytes);
                byteList.AddRange(metaBytes);
                var hash = FileHelper.GetMurmurHash3(byteList.ToArray());
                fileHashCacheMap[fileName] = hash;
                return hash;
            }
            catch (Exception ex)
            {
                Debug.LogError("hash计算错误:" + fileName);
                return "";
            }
        }


        #region 静态辅助函数

        #endregion
    }
}
