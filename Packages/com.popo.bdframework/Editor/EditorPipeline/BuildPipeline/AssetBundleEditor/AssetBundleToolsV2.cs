using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// AssetGraph构建AssetBundle
    /// </summary>
    static public class AssetBundleToolsV2
    {
        /// <summary>
        /// Runtime的定义
        /// </summary>
        static public string RUNTIME_PATH = "/runtime/";


        /// <summary>
        /// 文件hash缓存
        /// </summary>
        static Dictionary<string, string> fileHashCacheMap = new Dictionary<string, string>();


        /// <summary>
        /// Runtime的依赖资产列表
        /// </summary>
        static Dictionary<string, List<string>> DependenciesCacheMap = new Dictionary<string, List<string>>();


        /// <summary>
        /// 获取所有bd拓展的AssetGraph配置
        /// </summary>
        static public (ConfigGraph, NodeData) GetBDFrameExAssetGraph()
        {
            List<ConfigGraph> retList = new List<ConfigGraph>();

            var assets = AssetDatabase.FindAssets("t: UnityEngine.AssetGraph.DataModel.Version2.ConfigGraph", new string[1] {"Assets"});
            string envclsName = typeof(BDFrameworkAssetsEnv).FullName;
            foreach (var assetGuid in assets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var configGraph = AssetDatabase.LoadAssetAtPath<ConfigGraph>(assetPath);


                foreach (var node in configGraph.Nodes)
                {
                    if (node.Operation.Object is BDFrameworkAssetsEnv)
                    {
                        //含有bdenv节点的加入

                        return (configGraph, node);
                    }
                }
            }

            return (null, null);
        }


        #region 执行AssetGraph构建接口

        /// <summary>
        /// 执行AssetGraph构建打包
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outPath"></param>
        /// <param name="isUseHash"></param>
        static public void ExcuteAssetGraphBuild(BuildTarget buildTarget, string outPath)
        {
            var (cg, bdenvNode) = GetBDFrameExAssetGraph();
            var bdenv = (bdenvNode.Operation.Object as BDFrameworkAssetsEnv);
            bdenv.SetBuildParams(outPath, true);
            //执行
            AssetGraphUtility.ExecuteGraph(buildTarget, cg);
        }


        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isUseHashName">是否为hash name</param>
        public static bool GenAssetBundle(RuntimePlatform platform, string outputPath)
        {
            var buildTarget = BApplication.GetBuildTarget(platform);
            ExcuteAssetGraphBuild(buildTarget, outputPath);
            return true;
        }

        #endregion

        
        #region 构建BuildAssetInfo

        /// <summary>
        ///  获取BuildAssetsInfo
        /// </summary>
        /// <returns></returns>
        static public (BuildAssetInfos, List<string>) GetBuildingAssetInfos(BuildAssetInfos buildInfoCache = null)
        {
            var retBuildingInfo = new BuildAssetInfos();
            //开始
            var sw = new Stopwatch();
            sw.Start();

            retBuildingInfo.Time = DateTime.Now.ToShortDateString();
            int id = 0;
            //1.获取runtime资产
            var runtimeAssetsPathList = GetRuntimeAssetsPath();
            //2.搜集所有的依赖资产
            var allAssetList = new List<string>();
            allAssetList.AddRange(runtimeAssetsPathList);
            //获取所有依赖
            for (int i = 0; i < runtimeAssetsPathList.Count; i++)
            {
                var runtimeAsset = runtimeAssetsPathList[i];
                BuildAssetInfos.AssetInfo assetInfo = null;
                //从缓存取依赖
                buildInfoCache?.AssetInfoMap.TryGetValue(runtimeAsset, out assetInfo);
                if (assetInfo != null)
                {
                    allAssetList.AddRange(assetInfo.DependAssetList);
                }
                else
                {
                    var dependList = GetDependAssetList(runtimeAsset);
                    allAssetList.AddRange(dependList);
                }
            }

            //去重
            allAssetList = allAssetList.Distinct().ToList();
            //用所有资源构建AssetMap
            foreach (var assetPath in allAssetList)
            {
                //防止重复
                if (retBuildingInfo.AssetInfoMap.ContainsKey(assetPath))
                {
                    continue;
                }

                //优先从缓存获取
                var assetInfocache = buildInfoCache?.GetNewInstanceAssetInfo(assetPath);
                if (assetInfocache != null)
                {
                    //构建资源信息
                    retBuildingInfo.AddAsset(assetPath, assetInfocache);
                }
                else
                {
                    retBuildingInfo.AddAsset(assetPath);
                }
            }

            //TODO AB依赖关系纠正
            /// 已知Unity,bug/设计缺陷：
            ///   1.依赖接口，中会携带自己
            ///   2.如若a.png、b.png 依赖 c.atlas，则abc依赖都会是:a.png 、b.png 、 a.atlas
            // foreach (var asset in buildingAssetInfos.AssetDataMaps)
            // {
            //     //依赖中不包含自己
            //     //asset.Value.DependAssetList.Remove(asset.Key);
            // }
            sw.Stop();
            Debug.LogFormat("【GenBuildInfo】耗时:{0}ms.", sw.ElapsedMilliseconds);
            return (retBuildingInfo, runtimeAssetsPathList);
        }

        #endregion


        #region Runtime资源接口

        /// <summary>
        ///  获取runtime下的资源
        /// </summary>
        /// <returns></returns>
        static public List<string> GetRuntimeAssetsPath()
        {
            var allRuntimeDirects = BApplication.GetAllRuntimeDirects();
            var runtimeAssetPathList = new List<string>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //寻找所有Runtime的资产
            var runtimeGuids = AssetDatabase.FindAssets("", allRuntimeDirects.ToArray()).Distinct().ToList();
            foreach (var guid in runtimeGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).ToLower();
                path = IPath.ReplaceBackSlash(path);
                runtimeAssetPathList.Add(path);
            }

            runtimeAssetPathList = CheckAssetsPath(runtimeAssetPathList.ToArray());
            sw.Stop();
            Debug.Log($"GetRuntimeAssetsInfo耗时:{sw.ElapsedMilliseconds}ms");
            return runtimeAssetPathList;
        }


        /// <summary>
        /// 获取依赖资源
        /// </summary>
        /// <param name="mainAssetPath"></param>
        /// <returns></returns>
        static public string[] GetDependAssetList(string mainAssetPath)
        {
            List<string> dependList = null;
            if (!DependenciesCacheMap.TryGetValue(mainAssetPath, out dependList))
            {
                dependList = AssetDatabase.GetDependencies(mainAssetPath).Select((s) => IPath.ReplaceBackSlash(s).ToLower()).ToList();
                dependList.Remove(mainAssetPath);
                //检测依赖路径
                dependList = CheckAssetsPath(dependList.ToArray());
                //返回depend资源
                DependenciesCacheMap[mainAssetPath] = dependList;
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
                    //                  Debug.Log("【资源验证】移除目录资产" + path);
                    retList.RemoveAt(i);
                    continue;
                }

                //判断路径是否为editor依赖
                if (path.Contains("/editor/", StringComparison.OrdinalIgnoreCase) //一般的编辑器资源
                    || path.Contains("/Editor Resources/", StringComparison.OrdinalIgnoreCase) //text mesh pro的编辑器资源
                   )
                {
//                    Debug.Log("【资源验证】移除Editor资源" + path);
                    retList.RemoveAt(i);
                    //Debug.Log("【依赖验证】移除Editor资源" + path);
                    continue;
                }
            }

            return retList;
        }



        #endregion


        #region 资源变动检查

        /// <summary>
        /// 获取变动的Assets,通过对比文件hash
        /// </summary>
        static public BuildAssetInfos GetChangedAssetsByFileHash(string outputPath, BuildTarget buildTarget, BuildAssetInfos newBuildAssetInfos)
        {
            Debug.Log("<color=red>【增量资源】开始变动资源分析...</color>");
            BuildAssetInfos lastBuildAssetInfos = null;
            var buildinfoPath = IPath.Combine(outputPath, BApplication.GetPlatformPath(buildTarget), BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH);
            Debug.Log("旧资源地址:" + buildinfoPath);
            if (File.Exists(buildinfoPath))
            {
                var configContent = File.ReadAllText(buildinfoPath);
                lastBuildAssetInfos = JsonMapper.ToObject<BuildAssetInfos>(configContent);
            }


            //根据变动的list 刷出关联
            //I.单ab 单资源，直接重打
            //II.单ab 多资源的 整个ab都要重新打包
            if (lastBuildAssetInfos != null && lastBuildAssetInfos.AssetInfoMap.Count != 0)
            {
                #region 文件改动

                var changedAssetList = new List<KeyValuePair<string, BuildAssetInfos.AssetInfo>>();
                var changedAssetNameList = new List<string>();
                //1.找出差异文件：不一致  或者没有
                foreach (var newAssetItem in newBuildAssetInfos.AssetInfoMap)
                {
                    if (lastBuildAssetInfos.AssetInfoMap.TryGetValue(newAssetItem.Key, out var lastAssetItem))
                    {
                        //文件hash相同
                        if (lastAssetItem.Hash == newAssetItem.Value.Hash)
                        {
                            //依赖完全相同
                            var except = lastAssetItem.DependAssetList.Except(newAssetItem.Value.DependAssetList);
                            if (except.Count() == 0)
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
                var changedAssetBundleAssetList = new List<KeyValuePair<string, BuildAssetInfos.AssetInfo>>();
                //AB颗粒度
                var lastABUnitMap = lastBuildAssetInfos.PreviewAssetbundleUnit();
                var newABUnitMap = newBuildAssetInfos.PreviewAssetbundleUnit();
                //遍历处理
                foreach (var newAssetItem in newBuildAssetInfos.AssetInfoMap)
                {
                    if (lastBuildAssetInfos.AssetInfoMap.TryGetValue(newAssetItem.Key, out var lastAssetItem))
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
                            var except = lastABUnit.Except(newABUnit); //差集
                            if (except.Count() != 0)
                            {
                                changedAssetBundleAssetList.Add(newAssetItem);
                            }
                        }
                    }
                }


                Debug.LogFormat("<color=red>【增量资源】修改ABName(颗粒度)  影响文件数:{0}</color>", changedAssetBundleAssetList.Count);
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
                    foreach (var item in newBuildAssetInfos.AssetInfoMap)
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
                    var theSameABNameAssets = newBuildAssetInfos.AssetInfoMap.Where((asset) => asset.Value.ABName == rebuildABName);
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
                var allRebuildAssets = new List<KeyValuePair<string, BuildAssetInfos.AssetInfo>>();
                foreach (var abname in changedAssetNameList)
                {
                    var findAssets = newBuildAssetInfos.AssetInfoMap.Where((asset) => asset.Value.ABName == abname);
                    allRebuildAssets.AddRange(findAssets);
                }


                //去重
                var changedBuildInfo = new BuildAssetInfos();
                foreach (var kv in allRebuildAssets)
                {
                    changedBuildInfo.AssetInfoMap[kv.Key] = kv.Value;
                }

                Debug.LogFormat("<color=red>【增量资源】总重打资源数:{0}</color>", changedBuildInfo.AssetInfoMap.Count);
                var changedFiles = new List<string>();
                foreach (var item in changedBuildInfo.AssetInfoMap)
                {
                    changedFiles.Add(item.Key);
                }

                Debug.Log(JsonMapper.ToJson(changedFiles, true));


                var ablist = changedBuildInfo.AssetInfoMap.Values.Select((a) => a.ABName).Distinct().ToList();
                Debug.LogFormat("<color=red>【增量资源】变动Assetbundle:{0}</color>", ablist.Count);
                Debug.Log(JsonMapper.ToJson(ablist, true));

                return changedBuildInfo;
            }
            else
            {
                Debug.Log("<color=yellow>【增量资源】本地无资源，全部重打!</color>");
            }

            return newBuildAssetInfos;
        }


        /// <summary>
        /// 获取变动的Assets
        /// 通过SVN or git
        /// </summary>
        /// <returns></returns>
        static public void GetChangedAssetsByVCS(string lastVersionNum,string curVersionNum)
        {
        }

        #endregion


        #region Asset缓存、辅助等

        /// <summary>
        /// 资源导入缓存
        /// </summary>
        static private Dictionary<string, AssetImporter> AssetImpoterCacheMap = new Dictionary<string, AssetImporter>();

        /// <summary>
        /// 获取assetimpoter
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public AssetImporter GetAssetImporter(string path)
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
        static public void BackupArtifacts(BuildTarget platform)
        {
            var sourceDir = BApplication.Library + "/Artifacts";
            var targetDir = string.Format("{0}/{1}/Artifacts", BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformPath(platform));
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            Directory.CreateDirectory(targetDir);
            //复制整个目录
            FileHelper.CopyFolderTo(sourceDir, targetDir);
        }

        #endregion


        /// <summary>
        /// 获取主资源类型
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Type GetMainAssetTypeAtPath(string path)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            //图片类型得特殊判断具体的实例类型
            if (type == typeof(Texture2D))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null)
                {
                    return typeof(Sprite);
                }

                var tex2d = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex2d != null)
                {
                    return typeof(Texture2D);
                }

                var tex3d = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
                if (tex3d != null)
                {
                    return typeof(Texture3D);
                }
            }

            return type;
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
        
        
        #region Assetbundle混淆

        /// <summary>
        /// 获取混淆的资源
        /// </summary>
        /// <returns></returns>
        static public string[] GetMixAssets()
        {
            return AssetDatabase.FindAssets("t:TextAsset", new string[] {BResources.MIX_SOURCE_FOLDER});
        }

        /// <summary>
        /// 检测混淆资源
        /// </summary>
        static public void CheckABObfuscationSource()
        {
            var mixAsset = GetMixAssets();
            if (mixAsset.Length == 0)
            {
                Debug.LogError("【AssetBundle】不存在混淆源文件!");
            }
        }


        /// <summary>
        /// 添加混淆
        /// </summary>
        static public void MixAssetBundle(string outpath, RuntimePlatform platform)
        {
            var mixAssets = GetMixAssets();
            if (mixAssets.Length == 0)
            {
                Debug.LogError("【AssetBundle混淆】不存在混淆源文件!");
            }

            byte[][] mixSourceBytes = new byte[mixAssets.Length][];
            for (int i = 0; i < mixAssets.Length; i++)
            {
                var path = IPath.Combine(outpath, BApplication.GetPlatformPath(platform), BResources.ART_ASSET_ROOT_PATH, mixAssets[i]);
                var mixBytes = File.ReadAllBytes(path);
                mixSourceBytes[i] = mixBytes;
            }

            //构建ab管理器对象
            AssetBundleMgrV2 abv2 = new AssetBundleMgrV2();
            abv2.Init(outpath);
            //
            var mixAssetbundleItems = abv2.AssetConfigLoder.AssetbundleItemList.Where((i) => mixAssets.Contains(i.AssetBundlePath)).ToArray();

            Debug.Log("<color=green>--------------------开始混淆Assetbundle------------------------</color>");

            //开始混淆AssetBundle
            for (int i = 0; i < abv2.AssetConfigLoder.AssetbundleItemList.Count; i++)
            {
                //源AB
                var sourceItem = abv2.AssetConfigLoder.AssetbundleItemList[i];
                //非混合文件、ab不存在、mix过
                if (mixAssetbundleItems.Contains(sourceItem) || sourceItem.AssetBundlePath == null || sourceItem.Mix > 0)
                {
                    continue;
                }

                var idx = (int) (Random.Range(0, (mixAssetbundleItems.Length - 1) * 10000) / 10000);
                var mixBytes = mixSourceBytes[idx];
                //
                var abpath = IPath.Combine(outpath, BApplication.GetPlatformPath(platform), BResources.ART_ASSET_ROOT_PATH, sourceItem.AssetBundlePath);
                if (!File.Exists(abpath))
                {
                    Debug.LogError($"不存在AB:{sourceItem.AssetBundlePath} - {AssetDatabase.GUIDToAssetPath(sourceItem.AssetBundlePath)}");
                    continue;
                }

                var abBytes = File.ReadAllBytes(abpath);
                //拼接
                var outbytes = new byte[mixBytes.Length + abBytes.Length];
                Array.Copy(mixBytes, 0, outbytes, 0, mixBytes.Length);
                Array.Copy(abBytes, 0, outbytes, mixBytes.Length, abBytes.Length);
                //写入
                FileHelper.WriteAllBytes(abpath, outbytes);
                var hash = FileHelper.GetMurmurHash3(abpath);

                //相同ab的都进行赋值，避免下次重新被修改。
                foreach (var item in abv2.AssetConfigLoder.AssetbundleItemList)
                {
                    if (sourceItem.AssetBundlePath.Equals(item.AssetBundlePath))
                    {
                        item.Mix = mixBytes.Length;
                        item.Hash = hash;
                    }
                }

                //sourceItem.Mix = mixBytes.Length;

                //混淆
                Debug.Log("【Assetbundle混淆】" + sourceItem.AssetBundlePath);
            }

            //重新写入配置
            abv2.AssetConfigLoder.OverrideConfig();
            Debug.Log("<color=green>--------------------混淆Assetbundle完毕------------------------</color>");
        }

        #endregion


        #region 资产Hash
        
        /// <summary>
        /// 获取文件的md5
        /// 同时用资产+资产meta 取 hash
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static public string GetAssetsHash(string assetPath)
        {
            var str = "";
            if (fileHashCacheMap.TryGetValue(assetPath, out str))
            {
                return str;
            }

            try
            {
                //这里使用 asset + meta 生成hash,防止其中一个修改导致的文件变动 没更新
                var assetBytes = File.ReadAllBytes(assetPath);
                var metaBytes = File.ReadAllBytes(assetPath + ".meta");
                List<byte> byteList = new List<byte>();
                byteList.AddRange(assetBytes);
                byteList.AddRange(metaBytes);
                var hash = FileHelper.GetMurmurHash3(byteList.ToArray());
                fileHashCacheMap[assetPath] = hash;
                return hash;
            }
            catch (Exception ex)
            {
                Debug.LogError("hash计算错误:" + assetPath);
                return "";
            }
        }
        
        
        /// <summary>
        /// 获取一个AB中所有原资产的hash
        /// </summary>
        /// <returns></returns>
        static public string GetAnAssetbundleSourceAsetsHash()
        {
            return "";
        }
        #endregion

    }
}
