using Code.BDFramework.Core.Tools;
using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            /// 
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
            public string AB { get; set; } = "";

            /// <summary>
            /// 名字
            /// </summary>
            public string Name { get; set; } = "";

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
        /// <param name="outPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isHashName">是否为hash name</param>
        public static void GenAssetBundle(string outPath,
            BuildTarget target,
            BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression,
            bool isHashName = false,
            string AES="")
        {
            //
            var artOutpath = IPath.Combine(outPath, "Art");
            var builinfoPath = IPath.Combine(outPath, "BuildInfo.json");

            //初始化
            allfileHashMap = new Dictionary<string, string>();

            var assetPaths = BApplication.GetAllAssetsPath();
            for (int i = 0; i < assetPaths.Count; i++)
            {
                assetPaths[i] = assetPaths[i].ToLower();
            }

            /***********************新老资源依赖生成************************/
            //获取老的配置
            BuildInfo lastBuildInfo = new BuildInfo();
            if (File.Exists(builinfoPath))
            {
                var content = File.ReadAllText(builinfoPath);
                lastBuildInfo = JsonMapper.ToObject<BuildInfo>(content);
            }

            //获取当前配置
            var newbuildInfo = GetAssetsInfo(assetPaths);
            //获取变动的数据
            var changedAssetList = GetChangedAssets(lastBuildInfo, newbuildInfo);
            if (File.Exists(builinfoPath))
            {
                string targetPath = outPath + "/BuildInfo.old.json";
                File.Delete(targetPath);
                File.Move(builinfoPath, targetPath);
            }

            FileHelper.WriteAllText(builinfoPath, JsonMapper.ToJson(newbuildInfo));

            /***********************整理依赖关系 减少消耗************************/
            //保存buildinfo后,
            //整理runtime路径，减少加载时候的消耗
            var runtimeStr = "/runtime/";
            foreach (var asset in newbuildInfo.AssetDataMaps)
            {
                if (asset.Value.Name.Contains(runtimeStr))
                {
                    var newName = asset.Value.Name;
                    //移除runtime之前的路径
                    var index = newName.IndexOf(runtimeStr);
                    newName = newName.Substring(index + 1); //runtimeStr.Length);
                    //去除后缀
                    newName = newName.Replace(Path.GetExtension(newName), "");

                    //刷新整个列表替换
                    foreach (var _asset in newbuildInfo.AssetDataMaps)
                    {
                        var oldName = asset.Key.ToLower();
                        //name替换
                        if (_asset.Value.Name == oldName)
                        {
                            _asset.Value.Name = newName;
                        }

                        //ab替换
                        if (_asset.Value.AB == oldName)
                        {
                            _asset.Value.AB = newName;
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

            /***********************生成Config************************/
            //根据buildinfo 生成ArtConfig
            Dictionary<string, ManifestItem> configMap = new Dictionary<string, ManifestItem>();
            if (isHashName)
            {
                // foreach (var item in newbuildInfo.AssetDataMaps)
                // {
                //     var dependlist = new List<string>(item.Value.DependList.Count);
                //     for (int i = 0; i < dependlist.Count; i++)
                //     {
                //         var assetName = item.Value.DependList[i]; //
                //         var asset = newbuildInfo.AssetDataMaps[assetName];
                //         dependlist[i] = asset.Hash;
                //     }
                //
                //     //添加manifest
                //     var path = !string.IsNullOrEmpty(item.Value.AB) ? item.Value.AB : item.Key;
                //     var mi = new ManifestItem(path, (ManifestItem.AssetTypeEnum) item.Value.Type, dependlist);
                //     configMap[item.Key] = mi;
                // }
            }
            else
            {
                foreach (var item in newbuildInfo.AssetDataMaps)
                {
                    //添加manifest
                    var path = !string.IsNullOrEmpty(item.Value.AB) ? item.Value.AB : item.Value.Name;
                    var mi = new ManifestItem(path, (ManifestItem.AssetTypeEnum) item.Value.Type, new List<string>(item.Value.DependList));

                    //runtime路径下，改成用Resources加载规则命名的key
                    if (path.StartsWith("runtime/"))
                    {
                        configMap[item.Value.Name] = mi;
                    }
                    else 
                    {
                        configMap[item.Key] = mi;
                    }
                 
                }  
            }


            //hash命名

            //写入
            FileHelper.WriteAllText(artOutpath + "/Config.json", JsonMapper.ToJson(configMap));

            /***********************开始设置build ab************************/
            //设置AB name
            foreach (var asset in changedAssetList.AssetDataMaps)
            {
                string abname = "";
                if (!string.IsNullOrEmpty(asset.Value.AB))
                {
                    abname = asset.Value.AB;
                }
                else
                {
                    abname = asset.Value.Name;
                }

                var ai = GetAssetImporter(asset.Key);
                if (ai)
                {
                    ai.assetBundleName = abname;
                }
            }


            //3.生成AssetBundle
            BuildAssetBundle(target, outPath, options);
            //4.清除AB Name
            RemoveAllAssetbundleName();
            AssetImpoterCacheMap.Clear();
            //the end.删除无用文件
            var delFiles = Directory.GetFiles(artOutpath, "*", SearchOption.AllDirectories);
            foreach (var df in delFiles)
            {
                var ext = Path.GetExtension(df);
                if (ext == ".meta" || ext == ".manifest")
                {
                    File.Delete(df);
                }
            }
        }


        private static Dictionary<string, List<string>> atlasAssetsMap = null;

        #region 包颗配置

        //将指定后缀或指定文件,打包到一个AssetBundle
        public class MakePackage
        {
            public List<string> FileExtens = new List<string>();
            public string AssetBundleName = "noname";
        }

        static List<MakePackage> PackageConfigMap = new List<MakePackage>()
        {
            new MakePackage()
            {
                FileExtens = new List<string>() {".shader", ".shadervariants"},
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
            atlasAssetsMap = new Dictionary<string, List<string>>();
            //1.获取图集信息
            var atlas = paths.FindAll((p) => Path.GetExtension(p) == ".spriteatlas");
            for (int i = 0; i < atlas.Count; i++)
            {
                var asset = atlas[i];
                //获取依赖中的textrue
                var dps = GetDependencies(asset).ToList();
                atlasAssetsMap[asset] = dps;
            }

            //2.搜集Package config信息
            foreach (var config in PackageConfigMap)
            {
                var rets = paths.FindAll((p) => config.FileExtens.Contains(Path.GetExtension(p)));
                atlasAssetsMap[config.AssetBundleName] = rets.ToList();
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
                    asset.Name = subAssetPath;
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
                    foreach (var item in atlasAssetsMap)
                    {
                        if (item.Value.Contains(subAssetPath))
                        {
                            //设置AB的名字
                            asset.AB = item.Key;
                            break;
                        }
                    }

                    //自己规则的ab
                    foreach (var item in PackageConfigMap)
                    {
                        if (item.FileExtens.Contains(subAssetsExtension))
                        {
                            //设置AB的名字
                            asset.AB = item.AssetBundleName;
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
                asset.Value.DependList.Remove(asset.Value.Name);
            }

            //1.剔除图片会依赖图集的依赖,
            var atlasList =
                buildInfo.AssetDataMaps.Values.Where((a) => a.Type == (int) ManifestItem.AssetTypeEnum.SpriteAtlas);
            foreach (var _atlas in atlasList)
            {
                foreach (var img in _atlas.DependList)
                {
                    if (img == _atlas.Name)
                        continue;

                    var imgAsset = buildInfo.AssetDataMaps[img];
                    imgAsset.DependList.Clear();
                }
            }

            //2.把依赖资源替换成AB Name，
            foreach (var asset in buildInfo.AssetDataMaps.Values)
            {
                for (int i = 0; i < asset.DependList.Count; i++)
                {
                    var da = asset.DependList[i];
                    var dependAssetData = buildInfo.AssetDataMaps[da];
                    //替换成真正AB名
                    if (!string.IsNullOrEmpty(dependAssetData.AB))
                    {
                        asset.DependList[i] = dependAssetData.AB;
                    }
                }

                //去重
                asset.DependList = asset.DependList.Distinct().ToList();
            }

            return buildInfo;
        }


        /// <summary>
        ///TODO ： 待实现
        /// 获取改动的Assets
        /// </summary>
        static BuildInfo GetChangedAssets(BuildInfo lastInfo, BuildInfo newInfo)
        {
            var list = new List<BuildInfo.AssetData>();
            //比较 assetdata
            foreach (var item in newInfo.AssetDataMaps)
            {
                BuildInfo.AssetData lastAsset = null;
                //同名文件 hash不一样
                if (lastInfo.AssetDataMaps.TryGetValue(item.Key, out lastAsset))
                {
                    if (lastAsset.Hash != item.Value.Hash)
                    {
                        list.Add(item.Value);
                    }
                }
                else
                {
                    list.Add(item.Value);
                }
            }

            //根据变动的list 刷出关联
            //1.单ab 单资源，直接重打
            //2.单ab 多资源的 整个ab都要重新打包
            if (lastInfo.AssetDataMaps.Count != 0)
            {
            }

            return newInfo;
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

            BuildPipeline.BuildAssetBundles(path, options, target);
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
                    Debug.LogError("【打包】获取资源失败:" + path);
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
                //移除本身
                list = AssetDatabase.GetDependencies(path).Select((s) => s.ToLower()).ToList();
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
                var p = list[i];

                var fullPath = list[i];
                //
                if (!File.Exists(fullPath))
                {
                    list.RemoveAt(i);
                    continue;
                }

                //存在情况下 判断后缀
                var ext = Path.GetExtension(p).ToLower();
                if (ext == ".cs" || ext == ".js" || ext == ".dll")
                {
                    list.RemoveAt(i);
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