using Code.BDFramework.Core.Tools;
using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BDFramework.Editor.EditorLife;
using BDFramework.ResourceMgr.V2;
using UnityEditor;
using UnityEngine;


namespace BDFramework.Editor.Asset
{
    /// <summary>
    /// build信息
    /// </summary>
    public class BuildInfo
    {
        public class AssetData
        {
            /// <summary>
            /// Id
            /// </summary>
            public int Id { get; set; } = -1;

            /// <summary>
            /// 资源类型
            /// </summary>
            public int Type { get; set; } = -1;

            /// <summary>
            /// AssetBundleName
            /// 默认AB是等于自己文件名
            /// 当自己自己处于某个ab中的时候这个不为null
            /// </summary>
            public string ABName { get; set; } = "";


            /// <summary>
            /// 被依赖次数
            /// </summary>
            public int ReferenceCount { get; set; } = 0;
            
            /// <summary>
            /// hash
            /// </summary>
            public string Hash { get; set; } = "";

            /// <summary>
            /// 依赖列表
            /// </summary>
            public List<string> DependList { get; set; } = new List<string>();
            
            
        }

        /// <summary>
        /// time
        /// </summary>
        public string Time;

        /// <summary>
        /// 资源列表
        /// </summary>
        public Dictionary<string, AssetData> AssetDataMaps = new Dictionary<string, AssetData>();
    }

    static public class AssetBundleEditorToolsV2
    {
        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="outputPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isHashName">是否为hash name</param>
        public static void GenAssetBundle(string outputPath,
            RuntimePlatform platform,
            BuildTarget target,
            BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression,
            bool isHashName = false,
            string AES = "")
        {
            outputPath = Path.Combine(outputPath, BDApplication.GetPlatformPath(platform));
            //
            var artOutputPath = IPath.Combine(outputPath, "Art");
            var buildInfoPath = IPath.Combine(outputPath, "BuildInfo.json");
            //初始化
            allfileHashMap = new Dictionary<string, string>();
            var assetPaths = BDApplication.GetAllAssetsPath();
            for (int i = 0; i < assetPaths.Count; i++)
            {
                assetPaths[i] = assetPaths[i].ToLower();
            }

            /***********************新老资源依赖生成************************/
            //获取老的配置
            BuildInfo lastBuildInfo = new BuildInfo();
            if (File.Exists(buildInfoPath))
            {
                var content = File.ReadAllText(buildInfoPath);
                lastBuildInfo = JsonMapper.ToObject<BuildInfo>(content);
            }

            //获取当前配置
            var newbuildInfo = GetAssetsInfo(assetPaths);
            
            //BD生命周期触发
            BDFrameEditorBehaviorHelper.OnBeginBuildAssetBundle(newbuildInfo);
            
            //
            if (File.Exists(buildInfoPath))
            {
                string targetPath = outputPath + "/BuildInfo.old.json";
                File.Delete(targetPath);
                File.Move(buildInfoPath, targetPath);
            }
            FileHelper.WriteAllText(buildInfoPath, JsonMapper.ToJson(newbuildInfo));
            //获取改动的数据
            var changedBuildInfo = GetChangedAssets(lastBuildInfo, newbuildInfo);
            // newbuildInfo = null; //防止后面再用
            if (changedBuildInfo.AssetDataMaps.Count == 0)
            {
                Debug.Log("无资源改变,不需要打包!");
                return;
            }
            
            #region 整理依赖关系

            //1.把依赖资源替换成AB Name，
            foreach (var asset in newbuildInfo.AssetDataMaps.Values)
            {
                for (int i = 0; i < asset.DependList.Count; i++)
                {
                    var da = asset.DependList[i];
                    var dependAssetData = newbuildInfo.AssetDataMaps[da];
                    //替换成真正AB名
                    if (!string.IsNullOrEmpty(dependAssetData.ABName))
                    {
                        asset.DependList[i] = dependAssetData.ABName;
                    }
                }

                //去重
                asset.DependList = asset.DependList.Distinct().ToList();
                asset.DependList.Remove(asset.ABName);
            }

            //2.整理runtime路径 替换路径名为Resource规则的名字
            var runtimeStr = "/runtime/";
            foreach (var asset in newbuildInfo.AssetDataMaps)
            {
                if (asset.Key.Contains(runtimeStr))
                {
                    var newName = asset.Value.ABName;
                    //移除runtime之前的路径、后缀
                    var index = newName.IndexOf(runtimeStr);
                    newName = newName.Substring(index + 1); //runtimeStr.Length);

                    var extension = Path.GetExtension(newName);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        newName = newName.Replace(extension, "");
                    }

                    //刷新整个列表替换
                    foreach (var _asset in newbuildInfo.AssetDataMaps)
                    {
                        var oldName = asset.Key.ToLower();
                        //ab替换
                        if (_asset.Value.ABName == oldName)
                        {
                            _asset.Value.ABName = newName;
                        }

                        //依赖替换
                        for (int i = 0; i < _asset.Value.DependList.Count; i++)
                        {
                            if (_asset.Value.DependList[i] == oldName)
                            {
                                _asset.Value.DependList[i] = newName;
                            }
                        }
                    }
                }
            }

            #endregion


            #region 生成Runtime使用的Config

            //根据buildinfo 生成加载用的 Config
            //1.只保留Runtime目录下的配置
            Dictionary<string, ManifestItem> configMap = new Dictionary<string, ManifestItem>();
            if (isHashName)
            {
                
            }
            else
            {
                foreach (var item in newbuildInfo.AssetDataMaps)
                {
                    //runtime路径下，
                    //改成用Resources加载规则命名的key
                    if (item.Key.Contains("/runtime/"))
                    {
                        var key = item.Key;
                        //移除runtime之前的路径、后缀
                        var index = key.IndexOf(runtimeStr);
                        key = key.Substring(index + 1); //runtimeStr.Length);
                        key = key.Replace(Path.GetExtension(key), "");
                        //添加manifest
                        var mi = new ManifestItem( item.Value.ABName, (ManifestItem.AssetTypeEnum) item.Value.Type,
                            new List<string>(item.Value.DependList));
                        configMap[key] = mi;
                    }
                }
            }

            //写入
            FileHelper.WriteAllText(artOutputPath + "/Config.json", JsonMapper.ToJson(configMap));

            #endregion


            #region 设置ABname

            /***********************开始设置build ab************************/
            //设置AB name
            foreach (var changedAsset in changedBuildInfo.AssetDataMaps)
            {
                //根据改变的ChangedAssets,获取Asset的资源
                var key = changedAsset.Key;
                var asset = newbuildInfo.AssetDataMaps[changedAsset.Key];
                //设置ABName 有ab的则用ab ，没有就用configpath
                string abname = asset.ABName;
                //
                var ai = GetAssetImporter(key);
                if (ai)
                {
                    ai.assetBundleName = abname;
                }
            }

            #endregion


            //3.生成AssetBundle
            BuildAssetBundle(target, outputPath, options);
            //4.清除AB Name
            RemoveAllAssetbundleName();
            AssetImpoterCacheMap.Clear();
            //the end.删除无用文件
            var delFiles = Directory.GetFiles(artOutputPath, "*", SearchOption.AllDirectories);
            foreach (var df in delFiles)
            {
                var ext = Path.GetExtension(df);
                if (ext == ".meta" || ext == ".manifest")
                {
                    File.Delete(df);
                }
            }
            
            //BD生命周期触发
            BDFrameEditorBehaviorHelper.OnEndBuildAssetBundle(outputPath);
            
            AssetHelper.AssetHelper.GenPackageBuildInfo(outputPath,platform);
        }


        private static Dictionary<string, List<string>> packageAssetsMap = null;

        #region 包颗配置

        //将指定后缀或指定文件,打包到一个AssetBundle
        public class MakePackage
        {
            public List<string> FileExtens = new List<string>();
            public string AssetBundleName = "noname";
        }

        /// <summary>
        /// 包配置相关
        /// </summary>
        private static List<MakePackage> PackageConfigMap { get; set; } = new List<MakePackage>()
        {
            new MakePackage()
            {
                FileExtens = new List<string>() {".mat", ".shader", ".shadervariants"},
                AssetBundleName = "assets/shaders.ab"
            }
        };

        /// <summary>
        /// 资源类型配置
        /// </summary>
        static Dictionary<ManifestItem.AssetTypeEnum, List<string>> AssetTypeConfigMap =
            new Dictionary<ManifestItem.AssetTypeEnum, List<string>>()
            {
                {ManifestItem.AssetTypeEnum.Prefab, new List<string>() {".prefab"}}, //Prefab
                {ManifestItem.AssetTypeEnum.SpriteAtlas, new List<string>() {".spriteatlas"}}, //Atlas
            };

        #endregion

        /// <summary>
        /// 获取当前所有资源配置
        /// </summary>
        /// <returns></returns>
        static public BuildInfo GetAssetsInfo(List<string> paths)
        {
            packageAssetsMap = new Dictionary<string, List<string>>();
            //1.获取图集信息
            var atlas = paths.FindAll((p) => Path.GetExtension(p) == ".spriteatlas");
            for (int i = 0; i < atlas.Count; i++)
            {
                var asset = atlas[i];
                //获取依赖中的textrue
                var dps = GetDependencies(asset).ToList();
                packageAssetsMap[asset] = dps;
            }

            //2.搜集Package config信息
            foreach (var config in PackageConfigMap)
            {
                var rets = paths.FindAll((p) => config.FileExtens.Contains(Path.GetExtension(p)));
                packageAssetsMap[config.AssetBundleName] = rets.ToList();
            }

            //
            BuildInfo buildInfo = new BuildInfo();
            buildInfo.Time = DateTime.Now.ToShortDateString();
            int id = 0;
            //搜集所有的依赖
            foreach (var mainpath in paths)
            {
                var dependeAssetsPath = GetDependencies(mainpath);
                //获取依赖 并加入build info
                foreach (var subAssetPath in dependeAssetsPath)
                {
                    var asset = new BuildInfo.AssetData();
                    asset.Id = id;
                    asset.Hash = GetHashFromAssets(subAssetPath);
                    asset.ABName = subAssetPath;
                    //判断资源类型
                    asset.Type = (int) ManifestItem.AssetTypeEnum.Others;
                    var subAssetsExtension = Path.GetExtension(subAssetPath);
                    //
                    foreach (var item in AssetTypeConfigMap)
                    {
                        if (item.Value.Contains(subAssetsExtension))
                        {
                            asset.Type = (int) item.Key;
                            break;
                        }
                    }

                    //获取依赖
                    var dependeAssetList = GetDependencies(subAssetPath);
                    asset.DependList.AddRange(dependeAssetList);
                    //添加
                    buildInfo.AssetDataMaps[subAssetPath] = asset;
                    //判断package名
                    //图集
                    foreach (var item in packageAssetsMap)
                    {
                        if (item.Value.Contains(subAssetPath))
                        {
                            //设置AB的名字
                            asset.ABName = item.Key;
                            break;
                        }
                    }

                    //自己规则的ab
                    foreach (var item in PackageConfigMap)
                    {
                        if (item.FileExtens.Contains(subAssetsExtension))
                        {
                            //设置AB的名字
                            asset.ABName = item.AssetBundleName;
                            break;
                        }
                    }

                    id++;
                }
            }

            //TODO AB依赖关系纠正
            /// 已知Unity,bug/设计缺陷：
            ///   1.依赖接口，中会携带自己
            ///   2.如若a.png、b.png 依赖 c.atlas，则abc依赖都会是:a.png 、b.png 、 a.atlas
            foreach (var asset in buildInfo.AssetDataMaps)
            {
                //依赖中不包含自己
                asset.Value.DependList.Remove(asset.Value.ABName);
            }

            //2.Package Config信息添加到BuildInfo
            foreach (var config in PackageConfigMap)
            {
                var asset = new BuildInfo.AssetData();
                asset.ABName = config.AssetBundleName;
                //添加依赖
                var rets = buildInfo.AssetDataMaps.Values.Where((a) => a.ABName == config.AssetBundleName);
                asset.DependList.AddRange(rets.Select((r) => r.ABName));

                //压入列表
                buildInfo.AssetDataMaps[asset.ABName] = asset;
            }

            
            //搜集引用的次数，以runtime内部引用为主
            foreach (var item in buildInfo.AssetDataMaps)
            {
                if(!item.Key.Contains("/runtime/")) continue;
                //
                int count = 0;
                foreach (var assetdata in buildInfo.AssetDataMaps.Values)
                {
                    if (assetdata.DependList.Contains(item.Key))
                    {
                        count++;
                    }
                }
                item.Value.ReferenceCount = count;
            }

            return buildInfo;
        }


        /// <summary>
        /// 获取改动的Assets
        /// </summary>
        static BuildInfo GetChangedAssets(BuildInfo lastAssetsInfo, BuildInfo newAssetsInfo)
        {
            //根据变动的list 刷出关联
            //1.单ab 单资源，直接重打
            //2.单ab 多资源的 整个ab都要重新打包
            if (lastAssetsInfo.AssetDataMaps.Count != 0)
            {
                Debug.Log("<color=red>开始增量分析...</color>");
                var changedAssetList = new List<KeyValuePair<string, BuildInfo.AssetData>>();
                //找出差异文件
                foreach (var newAssetItem in newAssetsInfo.AssetDataMaps)
                {
                    BuildInfo.AssetData lastAssetData = null;
                    if (lastAssetsInfo.AssetDataMaps.TryGetValue(newAssetItem.Key, out lastAssetData))
                    {
                        if (lastAssetData.Hash == newAssetItem.Value.Hash)
                        {
                            continue;
                        }
                    }

                    changedAssetList.Add(newAssetItem);
                }

                Debug.LogFormat("<color=red>变动文件数:{0}</color>", changedAssetList.Count);
                //rebuild
                List<string> rebuildABNameList = new List<string>();
                foreach (var tempAsset in changedAssetList)
                {
                    //1.添加自身的ab
                    rebuildABNameList.Add(tempAsset.Value.ABName);
                    //2.添加所有依赖的ab
                    foreach (var depend in tempAsset.Value.DependList)
                    {
                        BuildInfo.AssetData dependAssetData = null;
                        if (newAssetsInfo.AssetDataMaps.TryGetValue(depend, out dependAssetData))
                        {
                            rebuildABNameList.Add(dependAssetData.ABName);
                        }
                        else
                        {
                            Debug.LogError("不存在资源:" + depend);
                        }
                    }
                }

                //去重
                rebuildABNameList = rebuildABNameList.Distinct().ToList();
                //搜索依赖的ab，直到没有新ab为止
                int counter = 0;
                while (counter < rebuildABNameList.Count )//防死循环
                {
                    string abName = rebuildABNameList[counter];
                    
                    var findAssets = newAssetsInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == abName);
                    foreach (var asset in findAssets)
                    {
                        //添加本体
                        var assetdata = newAssetsInfo.AssetDataMaps[asset.Key];
                        if (!rebuildABNameList.Contains(assetdata.ABName))
                        {
                            rebuildABNameList.Add(assetdata.ABName);
                        }
                        //添加依赖文件
                        foreach (var depend in assetdata.DependList)
                        {
                            BuildInfo.AssetData dependAssetData = null;
                            if (newAssetsInfo.AssetDataMaps.TryGetValue(depend, out dependAssetData))
                            {
                                if (!rebuildABNameList.Contains(dependAssetData.ABName))
                                {
                                    rebuildABNameList.Add(dependAssetData.ABName);
                                }
                            }
                            else
                            {
                                Debug.LogError("不存在资源:" + depend);
                            }
                        }
                    }

                    counter++;
                }

               
                
                var allRebuildAssets = new List<KeyValuePair<string, BuildInfo.AssetData>>();
                //根据影响的ab，寻找出所有文件
                foreach (var abname in rebuildABNameList)
                {
                    var findAssets = newAssetsInfo.AssetDataMaps.Where((asset) => asset.Value.ABName == abname);
                    allRebuildAssets.AddRange(findAssets);

                }


                //去重
                var retBuildInfo = new BuildInfo();
                foreach (var kv in allRebuildAssets)
                {
                    retBuildInfo.AssetDataMaps[kv.Key] = kv.Value;
                }

                Debug.LogFormat("<color=red>影响文件数:{0}</color>", retBuildInfo.AssetDataMaps.Count);

                return retBuildInfo;
            }


            return newAssetsInfo;
        }


        /// <summary>
        /// 创建assetbundle
        /// </summary>
        private static void BuildAssetBundle(BuildTarget target,
            string outPath,
            BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression)
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            string path = IPath.Combine(outPath, "Art");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            BuildPipeline.BuildAssetBundles(path, options | BuildAssetBundleOptions.DeterministicAssetBundle, target);
        }

        //当前保存的配置
        static ManifestConfig CurManifestConfig = null; //这个配置中 只会用Runtime的索引信息
        private static Dictionary<string, string> allfileHashMap = null;
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
                    if (path != "assets/shaders.ab")
                    {
                        Debug.LogError("【打包】获取资源失败:" + path);
                    }
                }
            }


            return ai;
        }


        #region 依赖关系

        static Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();

        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static private string[] GetDependencies(string path)
        {
            //全部小写
            //path = path.ToLower();
            List<string> list = null;
            if (!DependenciesMap.TryGetValue(path, out list))
            {
                list = AssetDatabase.GetDependencies(path).Select((s) => s.ToLower()).ToList();
                //检测依赖路径
                CheckAssetsPath(list);
                DependenciesMap[path] = list;
            }

            return list.ToArray();
        }

        /// <summary>
        /// 获取可以打包的资源
        /// </summary>
        /// <param name="allDependObjectPaths"></param>
        static private void CheckAssetsPath(List<string> list)
        {
            if (list.Count == 0)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var path = list[i];

                //文件不存在,或者是个文件夹移除
                if (!File.Exists(path) || Directory.Exists(path))
                {
                    list.RemoveAt(i);
                    continue;
                }

                //判断路径是否为editor依赖
                if (path.Contains("/editor/"))
                {
                    list.RemoveAt(i);
                    continue;
                }

                //特殊后缀
                var ext = Path.GetExtension(path).ToLower();
                if (ext == ".cs" || ext == ".js" || ext == ".dll")
                {
                    list.RemoveAt(i);
                    continue;
                }
            }
        }

        #endregion


        #region 文件 md5

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetHashFromAssets(string fileName)
        {
            var str = "";
            if (allfileHashMap.TryGetValue(fileName, out str))
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
                //这里为了防止碰撞 考虑Sha256 512 但是速度会更慢
                var sha1 = SHA1.Create();
                byte[] retVal = sha1.ComputeHash(byteList.ToArray());
                //hash
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                var hash = sb.ToString();
                allfileHashMap[fileName] = hash;
                return hash;
            }
            catch (Exception ex)
            {
                Debug.LogError("hash计算错误:" + fileName);
                return "";
            }
        }

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetHashFromString(string fileName)
        {
            var hash = "";
            if (allfileHashMap.TryGetValue(fileName, out hash))
            {
                return hash;
            }

            var sha1 = SHA1.Create();
            byte[] retVal = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fileName));
            //
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }

            allfileHashMap[fileName] = sb.ToString();

            return sb.ToString();
        }

        #endregion

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
                    ai.Value.assetBundleVariant = "";
                    ai.Value.assetBundleName = "";
                }
            }

            EditorUtility.ClearProgressBar();
        }


        public static void HashName2AssetName(string path)
        {
            string android = "Android";
            string iOS = "iOS";

            android = IPath.Combine(path, android);
            iOS = IPath.Combine(path, iOS);

            string[] paths = new string[] {android, iOS};

            foreach (var p in paths)
            {
                if (!Directory.Exists(p))
                {
                    Debug.Log("不存在:" + p);
                    continue;
                }

                var cachePath = IPath.Combine(p, "Art/Cache.json");
                var cacheDic = JsonMapper.ToObject<Dictionary<string, string>>(File.ReadAllText(cachePath));

                float i = 0;
                foreach (var cache in cacheDic)
                {
                    var source = IPath.Combine(p, "Art/" + cache.Value);
                    var index = cache.Key.IndexOf("/Assets/");
                    string t = "";
                    if (index != -1)
                    {
                        t = cache.Key.Substring(index);
                    }
                    else
                    {
                        t = cache.Key;
                    }

                    var target = IPath.Combine(p, "ArtEditor/" + t);
                    if (File.Exists(source))
                    {
                        FileHelper.WriteAllBytes(target, File.ReadAllBytes(source));
                    }

                    i++;
                    EditorUtility.DisplayProgressBar("进度", i + "/" + cacheDic.Count, i / cacheDic.Count);
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("还原完成!");
        }
    }
}