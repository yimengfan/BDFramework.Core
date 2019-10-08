using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BDFramework.Core.Debugger;
using BDFramework.ResourceMgr;
using LitJson;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEditor;
using FileMode = System.IO.FileMode;

namespace BDFramework.Editor.Asset
{
    static public class AssetBundleEditorTools
    {
        /// <summary>
        /// 检测资源
        /// </summary>
        /// <param name="resRootPath"></param>
        /// <param name="outPath"></param>
        /// <param name="target"></param>
        public static void CheackAssets(string resRootPath, string outPath, BuildTarget target)
        {
            //1.分析资源
            string rootPath = IPath.Combine(Application.dataPath, resRootPath);
            //扫描所有文件
            var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
            var fileList = new List<string>(allFiles);
            //剔除不打包的部分
            for (int i = fileList.Count - 1; i >= 0; i--)
            {
                var fi = allFiles[i];
                var extension = Path.GetExtension(fi.ToLower());
                //
                if (extension.ToLower() == ".meta" || extension.ToLower() == ".cs" || extension.ToLower() == ".js")
                {
                    fileList.RemoveAt(i);
                }
            }

            AnalyzeResource(fileList.ToArray(), target, IPath.Combine(outPath, "Art"));

            //2.配置写入本地

            var configPath = IPath.Combine(outPath, "Art/Config_Check.json");
            var direct = Path.GetDirectoryName(configPath);
            if (Directory.Exists(direct) == false)
            {
                Directory.CreateDirectory(direct);
            }

            File.WriteAllText(configPath, CurManifestConfig.ToString());
        }


        //
        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="resRootPath"></param>
        /// <param name="outPath"></param>
        /// <param name="target"></param>
        public static void GenAssetBundle(string resRootPath, string outPath, BuildTarget target,
            BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression)
        {
            //0.cache path的路径
            cachePath = IPath.Combine(outPath, "Art/Cache.json");
            configPath = IPath.Combine(outPath, "Art/Config.json");
            //1.环境准备
            string rootPath = IPath.Combine(Application.dataPath, resRootPath);
            //扫描所有文件
            var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
            var fileList = new List<string>(allFiles);
            //剔除不打包的部分
            for (int i = fileList.Count - 1; i >= 0; i--)
            {
                var fi = allFiles[i];
                var extension = Path.GetExtension(fi.ToLower());
                //
                if (extension.ToLower() == ".meta" || extension.ToLower() == ".cs" || extension.ToLower() == ".js")
                {
                    fileList.RemoveAt(i);
                }
            }

            //2.分析ab包
            AnalyzeResource(fileList.ToArray(), target, IPath.Combine(outPath, "Art"));
            //3.生成AssetBundle
            BuildAssetBundle(target, outPath, options);

            //保存配置
            FileHelper.WriteAllText(configPath, CurManifestConfig.ToString());
            //保存Cache.json
            FileHelper.WriteAllText(cachePath, JsonMapper.ToJson(allfileHashMap, true));

            //4.清除AB Name
            RemoveAllAbName();
            //删除无用文件
            var delFiles = Directory.GetFiles(outPath, "*", SearchOption.AllDirectories);
            foreach (var df in delFiles)
            {
                var ext = Path.GetExtension(df);
                if (ext == ".meta" || ext == ".manifest")
                {
                    File.Delete(df);
                }

                //避免删除配置
                if (df.EndsWith("Cache.json") || df.EndsWith("Config.json"))
                {
                    continue;
                }

                //
                var fn = Path.GetFileName(df);
                var item = CurManifestConfig.GetManifestItemByHash(fn);
                if (item == null)
                {
                    File.Delete(df);
                }
            }
        }


        /// <summary>
        /// 创建assetbundle
        /// </summary>
        private static void BuildAssetBundle(BuildTarget target, string outPath,
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


        #region 包颗粒度配置

        //将指定后缀或指定文件,打包到一个AssetBundle
        public class MakePackage
        {
            public List<string> fileExtens = new List<string>();
            public string AssetBundleName = "noname";
        }

        static List<MakePackage> PackageConfig = new List<MakePackage>()
        {
            new MakePackage()
            {
                fileExtens = new List<string>() {".shader", ".shadervariants"},
                AssetBundleName = "ALLShaders.ab"
            }
        };

        #endregion

        //当前保存的配置
        static ManifestConfig CurManifestConfig = null; //这个配置中 只会用Runtime的索引信息
        static List<string> additionBuildPackageCache = new List<string>();
        private static string cachePath = "";
        private static string configPath = "";
        private static Dictionary<string, string> allfileHashMap = null;

        /// <summary>
        /// 分析资源
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="target"></param>
        /// <param name="outpath"></param>
        private static void AnalyzeResource(string[] paths, BuildTarget target, string outpath)
        {
            additionBuildPackageCache = new List<string>();
            CurManifestConfig = new ManifestConfig();
            //以下三个建议使用内部函数，不要直接调用unity原生接口获取，
            allfileHashMap = new Dictionary<string, string>(); //file hash获取缓存
            assetImpoterMap = new Dictionary<string, AssetImporter>(); //Assetimport获取缓存
            DependenciesMap = new Dictionary<string, List<string>>(); //依赖获取缓存

            //加载配置
            ManifestConfig lastManifestConfig = null;
            if (File.Exists(configPath)) lastManifestConfig = new ManifestConfig(File.ReadAllText(configPath));
            else lastManifestConfig = new ManifestConfig();
            //

            /***************************************开始分析资源****************************************/
            //1.收集图集信息
            EditorUtility.DisplayProgressBar("分析资源", "收集SpriteAtlas", 0);
            GetSpriteAtlasInfo();
            //2.收集单ab多资源信息
            GetBuildAbConfig();
            //3.开始分析资源
            bool isAdditionBuild = allfileHashMap.Count > 0; //是否为增量打包
            List<string> changeList = new List<string>();
            for (int index = 0; index < paths.Length; index++)
            {
                var mainAssetFullPath = paths[index].Replace("\\", "/");
                EditorUtility.DisplayProgressBar("分析资源",
                    string.Format("分析:{0} {1}/{2}", Path.GetFileName(mainAssetFullPath), index + 1, paths.Length),
                    (index + 1f) / paths.Length);
                //获取被依赖的路径
                var mainAssetPath = "Assets" + mainAssetFullPath.Replace(Application.dataPath, "");
                var subAssetsPath = GetDependencies(mainAssetPath).ToList();


                List<string> subAssetHashList = new List<string>();
                //处理依赖资源打包
                for (int i = 0; i < subAssetsPath.Count; i++)
                {
                    var subAsset = subAssetsPath[i];
                    var subAssetPath = Application.dataPath + subAsset.Replace("Assets/", "/");
                    string subAssetHash = GetHashFromFile(subAssetPath);
                    subAssetHashList.Add(subAssetHash);

                    //本地ab文件存在则不打包
                    var localABPath = IPath.Combine(outpath, subAssetHash);
                    if (File.Exists(localABPath))
                    {
                        var lastItem = lastManifestConfig.GetManifestItemByHash(subAssetHash);
                        if (lastItem != null)
                        {
                            CurManifestConfig.AddItem(lastItem);
                            Debug.Log("跳过:" + subAsset);
                            continue;
                        }
                    }
                    else
                    {
                        // 需要对比之前的hash
                        var lastItem = lastManifestConfig.GetManifestItemByHash(subAssetHash);
                        if (lastItem != null && lastItem.Hash == subAssetHash)
                        {
                            CurManifestConfig.AddItem(lastItem);
                            Debug.Log("跳过:" + subAsset);
                            continue;
                        }
                    }

                    #region 嵌套引用 - 缓存

                    var subAssetDpendList = GetDependencies(subAsset).ToList();
                    //sub dpend 2 hash
                    if (subAssetDpendList.Count > 1)
                    {
                        for (int j = 0; j < subAssetDpendList.Count; j++)
                        {
                            var sbd = Application.dataPath + subAssetDpendList[j].Replace("Assets/", "/");
                            subAssetDpendList[j] = GetHashFromFile(sbd);
                        }
                    }
                    else
                    {
                        subAssetDpendList.Clear();
                    }

                    #endregion

                    //开始设置abname 
                    var ai = GetAssetImporter(subAsset);
                    string packageHashName = null;

                    #region 单ab多资源模式

                    if (IsMakePackage(subAsset, ref packageHashName))
                    {
                        #region 增量打包遇到单ab多资源

                        //增量打包时，如果遇到多包合一时，其中某个变动，剩余的也要一次性打出
//                        if (isAdditionBuild && !additionBuildPackageCache.Contains(packageHashName))
//                        {
//                            var lastAssets = lastManifestConfig.Manifest_NameKey.Values.ToList().FindAll((item) =>
//                                !string.IsNullOrEmpty(item.Package) && item.Package == packageHashName);
//                            foreach (var la in lastAssets)
//                            {
//                                //考虑增量打包时候,得补齐Runtime下的路径名
//                                var path = la.Name;
//                                if (!path.StartsWith("Assets/"))
//                                {
//                                    foreach (var key in LastAllAssetCache.Keys)
//                                    {
//                                        var p = path + ".";
//                                        if (key.Contains(p))
//                                        {
//                                            path = key;
//                                        }
//                                    }
//                                }
//
//                                //获取上次的importer
//                                var laAI = GetAssetImporter(path);
//                                if (laAI == null)
//                                {
//                                    Debug.LogError("资源不存在:" + la.Name);
//                                    continue;
//                                }
//
//                                laAI.assetBundleName = packageHashName;
//                                laAI.assetBundleVariant = "";
//                            }
//
//                            if (isAdditionBuild)
//                            {
//                                additionBuildPackageCache.Add(packageHashName);
//                            }
//                        }

                        #endregion

                        //
                        ai.assetBundleName = packageHashName;
                        ai.assetBundleVariant = "";
                        if (subAsset != mainAssetPath)
                        {
                            ManifestItem.AssetTypeEnum @enum = ManifestItem.AssetTypeEnum.Others;
                            var savename = CheckAssetSaveInfo(subAsset, ref @enum);
                            CurManifestConfig.AddItem(savename, subAssetHash, subAssetDpendList, @enum,
                                packageHashName);
                        }
                    }

                    #endregion

                    else
                    {
                        ai.assetBundleName = subAssetHash;
                        ai.assetBundleVariant = "";
                        if (subAsset != mainAssetPath)
                        {
                            ManifestItem.AssetTypeEnum @enum = ManifestItem.AssetTypeEnum.Others;
                            var savename = CheckAssetSaveInfo(subAsset, ref @enum);
                            CurManifestConfig.AddItem(savename, subAssetHash, subAssetDpendList, @enum);
                        }
                    }

                    changeList.Add(subAsset);
                }

                //最后保存主文件
                var mainHash = GetHashFromFile(mainAssetFullPath);
                string package = null;
                subAssetHashList.Remove(mainHash);
                if (IsMakePackage(mainAssetPath, ref package))
                {
                    //单ab包-多资源模式
                    ManifestItem.AssetTypeEnum @enum = ManifestItem.AssetTypeEnum.Others;
                    var sn = CheckAssetSaveInfo(mainAssetPath, ref @enum);
                    CurManifestConfig.AddItem(sn, mainHash, subAssetHashList, @enum, package);
                }
                else
                {
                    //单ab包-单资源模式
                    ManifestItem.AssetTypeEnum @enum = ManifestItem.AssetTypeEnum.Others;
                    var sn = CheckAssetSaveInfo(mainAssetPath, ref @enum);
                    CurManifestConfig.AddItem(sn, mainHash, subAssetHashList, @enum);
                }
            }

            //补全 [单ab多资源的配置],并将真正的hash传入
            foreach (var con in PackageConfig)
            {
                var hash = GetHashFromString(con.AssetBundleName); //多合一ab的hash是要有所有的依赖文件 hash,再hash一次
                CurManifestConfig.AddItem(con.AssetBundleName, hash, new List<string>(),
                    ManifestItem.AssetTypeEnum.Others);
            }

            //最后检查配置
            foreach (var item in CurManifestConfig.Manifest_HashKey.Values)
            {
                for (int i = 0; i < item.Depend.Count; i++)
                {
                    var dHash = item.Depend[i];
                    //判断是否在runtime内
                    var dItem = CurManifestConfig.GetManifestItemByHash(dHash);

                    if (dItem != null)
                    {
                        if (!string.IsNullOrEmpty(dItem.Package))
                        {
                            //将非Runtime目录中的 
                            item.Depend[i] = dItem.Package;
                        }
                    }
                    else
                    {
                        Debug.LogError("【资源遗失】没找到依赖项:" + dHash);
                        foreach (var v in allfileHashMap)
                        {
                            if (dHash == v.Value)
                            {
                                Debug.LogError("hash source file:" + v.Key);
                                break;
                            }
                        }
                    }
                }

                item.Depend.Remove(item.Hash);
                item.Depend = item.Depend.Distinct().ToList();
            }

            EditorUtility.ClearProgressBar();
            changeList = changeList.Distinct().ToList();
            Debug.LogFormat("<color=red>本地需要打包数量:{0}</color>", changeList.Count);
            var buidpath = string.Format("{0}/{1}_changelist.json", Application.dataPath, target.ToString());
            Debug.Log("本地打包保存:" + buidpath);
        }


        /// <summary>
        /// 获取可以打包的资源
        /// </summary>
        /// <param name="allDependObjectPaths"></param>
        static private void CheckAssetsPath(ref List<string> list)
        {
            if (list.Count == 0) return;


            for (int i = list.Count - 1; i >= 0; i--)
            {
                var p = list[i];

                var fullPath = Application.dataPath + p.Replace("Assets/", "/");
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


        static private Dictionary<string, AssetImporter> assetImpoterMap = new Dictionary<string, AssetImporter>();

        /// <summary>
        /// 获取assetimpoter
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static private AssetImporter GetAssetImporter(string path)
        {
            AssetImporter ai = null;
            if (!assetImpoterMap.TryGetValue(path, out ai))
            {
                ai = AssetImporter.GetAtPath(path);
                assetImpoterMap[path] = ai;
            }

            return ai;
        }


        static Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();

        static private string[] GetDependencies(string path)
        {
            List<string> list = null;

            if (!DependenciesMap.TryGetValue(path, out list))
            {
                list = AssetDatabase.GetDependencies(path).ToList();

                //1.第一种情况,图集子图依赖 会包含所有的 
                var ret = list.Find((s) => s.EndsWith(".spriteatlas"));
                //依赖中有图集
                if (ret != null && !path.EndsWith(".spriteatlas"))
                {
                    //图集中依赖包含自己
                    var atlasDependencies = GetDependencies(ret);
                    if (atlasDependencies.Contains(path))
                    {
                        list.Clear();
                        list.Add(path);
                    }
                }

                //
                DependenciesMap[path] = list;
            }

            //
            var retList = new List<string>(list);
            CheckAssetsPath(ref retList);
            return retList.ToArray();
        }


        #region 图集相关

        private static Dictionary<string, List<string>> atlasMap = null;
        private static HashSet<string> textureExtensionSet = null;

        /// <summary>
        /// 收集图集信息
        /// </summary>
        static private void GetSpriteAtlasInfo()
        {
            atlasMap = new Dictionary<string, List<string>>();
            textureExtensionSet = new HashSet<string>();
            //
            var path = "Assets/Resource/Runtime";
            var assets = AssetDatabase.FindAssets("t:spriteatlas", new string[] {path}).ToList();

            //GUID to assetPath
            for (int i = 0; i < assets.Count; i++)
            {
                var p = AssetDatabase.GUIDToAssetPath(assets[i]).ToLower();
                //获取依赖中的textrue
                var dps = GetDependencies(p).ToList();
                atlasMap[p] = dps;
                for (int j = 0; j < dps.Count; j++)
                {
                    textureExtensionSet.Add(Path.GetExtension(dps[j]));
                }
            }
        }

        /// <summary>
        /// 获取打包怕配置
        /// </summary>
        static private void GetBuildAbConfig()
        {
        }

        /// <summary>
        /// 获取文件的md5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetHashFromFile(string fileName)
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
                Debug.LogError("hash计算错误:" + fileName.Replace(Application.dataPath, "Assets"));
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
        /// 是否需要多包合一 ,后期可以重写这里，让配置规则 可以更健壮
        /// </summary>
        /// <param name="name"></param>
        /// <param name="package"></param>
        /// <returns>返回Package的hashname</returns>
        static private bool IsMakePackage(string name, ref string package)
        {
            var ext = Path.GetExtension(name);
            //图集
            if (textureExtensionSet.Contains(ext))
            {
                foreach (var item in atlasMap)
                {
                    //收集图片是否被图集引用
                    if (item.Value.Contains(name))
                    {
                        package = GetHashFromFile(item.Key);

                        return true;
                    }
                }
            }

            //多资源单ab的配置 
            foreach (var config in PackageConfig)
            {
                foreach (var exten in config.fileExtens)
                {
                    if (name.EndsWith(exten))
                    {
                        package = GetHashFromString(config.AssetBundleName);
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// 创建存储的名字
        /// 主要是用于 Runtime下和Runtime外的区分
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static public string CheckAssetSaveInfo(string str, ref ManifestItem.AssetTypeEnum @typeEnum)
        {
            //判断类型
            var ext = Path.GetExtension(str).ToLower();
            if (ext.Equals(".prefab"))
            {
                typeEnum = ManifestItem.AssetTypeEnum.Prefab;
            }
            else if (ext.Equals(".spriteatlas"))
            {
                typeEnum = ManifestItem.AssetTypeEnum.SpriteAtlas;
            }
            else
            {
                typeEnum = ManifestItem.AssetTypeEnum.Others;
            }

            //判断是否在Runtime中
            //Runtime中要掐头去尾
            if (str.StartsWith("Assets/Resource/Runtime/"))
            {
                str = str.Replace("Assets/Resource/Runtime/", "");
                if (!string.IsNullOrEmpty(ext))
                {
                    str = str.Replace(ext, "");
                }
            }

            return str;
        }

        /// <summary>
        /// 移除无效资源
        /// </summary>
        public static void RemoveAllAbName()
        {
            EditorUtility.DisplayProgressBar("资源清理", "清理中...", 1);

            foreach (var ai in assetImpoterMap)
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
                    var target = IPath.Combine(p, "ArtEditor/" +t);
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