using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using BDFramework.ResourceMgr;
using LitJson;
using UnityEngine;
using UnityEditor;
using FileMode = System.IO.FileMode;


public class CacheItem
{
    public string Name = "null";
    public string UIID = "none";
}

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

        AnalyzeResource(fileList.ToArray(), target, outPath);

        //2.配置写入本地

        var configPath = IPath.Combine(outPath, "Art/Config_Check.json");
        var direct = Path.GetDirectoryName(configPath);
        if (Directory.Exists(direct) == false)
        {
            Directory.CreateDirectory(direct);
        }

        File.WriteAllText(configPath, curManifestConfig.ToString());
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
        AnalyzeResource(fileList.ToArray(), target, outPath);
        
        //3.生成AssetBundle
        BuildAssetBundle(target, outPath, options);
        
        //保存配置
        FileHelper.WriteAllText(configPath, curManifestConfig.ToString());
        //保存Cache.json
        FileHelper.WriteAllText(cachePath,AssetCache.ToString());

        //删除无用文件
        var delFiles = Directory.GetFiles(outPath, "*.*", SearchOption.AllDirectories);
        foreach (var df in delFiles)
        {
            var ext = Path.GetExtension(df);
            if (ext == ".meta" || ext == ".manifest")
            {
                File.Delete(df);
            }
        }        
        //4.清除AB Name
        RemoveAllAbName();
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
            AssetBundleName = "ALLShader.assetbundle"
        }
    };
    
    #endregion
    //当前保存的配置
    static ManifestConfig curManifestConfig = null;
    static  List<string>additionBuildPackageCache = new List<string>();
    private static ManifestConfig AssetCache;
    private static string cachePath = "";
    private static string configPath = "";
    /// <summary>
    /// 分析资源
    /// </summary>
    /// <param name="paths"></param>
    /// <param name="target"></param>
    /// <param name="outpath"></param>
    private static void AnalyzeResource(string[] paths, BuildTarget target, string outpath)
    {
        additionBuildPackageCache = new List<string>();
        curManifestConfig = new ManifestConfig();
        //加载

        ManifestConfig lastManifestConfig = null;
        if (File.Exists(configPath))
        {
            lastManifestConfig = new ManifestConfig(File.ReadAllText(configPath));
        }
        else
        {
            lastManifestConfig = new ManifestConfig();
        }
        
        //
        if (File.Exists(cachePath))
        {
            AssetCache = new ManifestConfig(File.ReadAllText(cachePath));
        }
        else
        {
            AssetCache = new ManifestConfig();
        }
        

        /***************************************开始分析资源****************************************/
        List<string> changeList = new List<string>();
        float curIndex = 0;


        var allAssetList = paths.ToList();
        for (int index = 0; index < allAssetList.Count; index++)
        {
            var path = allAssetList[index];
            
            var _path = path.Replace("\\", "/");

            EditorUtility.DisplayProgressBar(
                "分析资源 -" + target,
                "分析:" + Path.GetFileNameWithoutExtension(_path) + "   进度：" + curIndex + "/" + paths.Length,
                curIndex / paths.Length);
            curIndex++;
            

            //获取被依赖的路径
            var dependsource = "Assets" + _path.Replace(Application.dataPath, "");
            var allDependObjectPaths = AssetDatabase.GetDependencies(dependsource).ToList();
            dependsource = dependsource.ToLower();

            GetCanBuildAssets(ref allDependObjectPaths);
            
            //处理依赖资源打包
            for (int i = 0; i < allDependObjectPaths.Count; i++)
            {
                var dp = allDependObjectPaths[i];
                var dependObjPath = Application.dataPath + dp.TrimStart("Assets".ToCharArray());
                var uiid = GetMD5HashFromFile(dependObjPath);
                //判断是否打包
                ManifestItem lastItem = null;
                AssetCache.Manifest.TryGetValue(dp, out lastItem);
                //已经添加,不用打包
                if (lastItem !=null && lastItem.UIID == uiid)
                {
                   //不用打包记录缓存
                   var _last = lastManifestConfig.Manifest.Values.ToList().Find((item) => item.UIID == lastItem.UIID);
                   if(_last!=null)
                   curManifestConfig.AddDepend(_last.Name, _last.UIID, _last.Dependencies, _last.PackageName);
                   continue;
                }
                changeList.Add(dependsource);
                //
                AssetCache.AddDepend(dp, uiid, new List<string>());
                //开始设置abname  用以打包
                AssetImporter ai = AssetImporter.GetAtPath(dp);
                string abname = "Assets" + dependObjPath.Replace(Application.dataPath, "");
                //判断是否要打在同一个ab包内
                string packageName = null;
                var list = new List<string>();
                //嵌套引用prefab
                if (dp.ToLower()!=dependsource && Path.GetExtension(dp).ToLower().Equals(".prefab"))
                {
                    list =  AssetDatabase.GetDependencies(abname).ToList();
                    GetCanBuildAssets(ref list);

                    //转换成全小写
                    for (int j = 0; j < list.Count; j++)
                    {
                        list[j] = list[j].ToLower();
                    }
                }
                
                abname = abname.ToLower();
                if (IsMakePackage(abname, ref packageName))
                {
                    
                    //增量打包时，如果遇到多包合一时，其中某个变动，剩余的也要一次性打出
                    if (!additionBuildPackageCache.Contains(packageName))
                    {
                        var lowPackgeName = packageName.ToLower();
                        var oldAsset = lastManifestConfig.Manifest.Values.ToList().FindAll((item) => item.PackageName == lowPackgeName);
                        foreach (var oa in oldAsset)
                        {
                            AssetImporter _ai = AssetImporter.GetAtPath(oa.Name);
                            if (_ai == null)
                            {
                                Debug.LogError("资源不存在:" + oa.Name);
                                continue;
                            }

                            _ai.assetBundleName = packageName;
                            _ai.assetBundleVariant = "";
                        }
                        Debug.LogFormat("<color=yellow>重新打包:{0} , 依赖:{1}</color>",packageName,oldAsset.Count);
                        additionBuildPackageCache.Add(packageName);
                    }
                    //
                    ai.assetBundleName = packageName;
                    ai.assetBundleVariant = "";
                    //被依赖的文件,不保存其依赖信息
                    if (abname != dependsource) 
                    {
                        curManifestConfig.AddDepend(abname, uiid,list , packageName.ToLower());
                    }
                }
                else
                {
                    ai.assetBundleName = abname;
                    ai.assetBundleVariant = "";
                    //被依赖的文件,不保存其依赖信息
                    if (abname != dependsource) //依赖列表中会包含自己
                    {
                        curManifestConfig.AddDepend(abname, uiid, list);
                    }
                }

            }

           
            //保存主文件的依赖
            {
                //获取MD5的UIID
                var UIID = GetMD5HashFromFile(_path);
                allDependObjectPaths.Remove(dependsource);
                for (int i = 0; i < allDependObjectPaths.Count; i++)
                {
                    allDependObjectPaths[i] = allDependObjectPaths[i].ToLower();
                }
                //
                string packageName = null;
                if (IsMakePackage(dependsource, ref packageName))
                {
                    //单ab包-多资源模式
                    curManifestConfig.AddDepend(dependsource, UIID, allDependObjectPaths, packageName.ToLower());
                }
                else
                {
                    //单ab包-单资源模式
                    curManifestConfig.AddDepend(dependsource, UIID, allDependObjectPaths);
                }
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log("本地需要打包资源:" + changeList.Count);
        if (changeList.Count < 100)
        {
            Debug.Log("本地需要打包资源:" + JsonMapper.ToJson(changeList));
        }
    }



    /// <summary>
    /// 获取可以打包的资源
    /// </summary>
    /// <param name="allDependObjectPaths"></param>
    static private void GetCanBuildAssets(ref List<string>  list)
    {
        if(list.Count==0)  return;
        
        for (int i = list.Count-1; i >= 0; i--)
        {
            var p = list[i];
            var ext = Path.GetExtension(p).ToLower();
            //
            var fullPath = Application.dataPath + p.TrimStart("Assets".ToCharArray());
            if (ext != ".cs" && ext != ".js" && ext != ".dll"
              && File.Exists(fullPath) )
            {
                continue;
            }
            //
            list.RemoveAt(i);
        }
    }
    
    /// <summary>
    /// 是否需要打包
    /// </summary>
    /// <param name="name"></param>
    /// <param name="package"></param>
    /// <returns></returns>
    static private bool IsMakePackage(string name, ref string package)
    {
        foreach (var config in PackageConfig)
        {
            foreach (var exten in config.fileExtens)
            {
                if (name.EndsWith(exten))
                {
                    package = config.AssetBundleName;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 保存每次Cache,与下次做差异对比使用
    /// </summary>
    /// <param name="saveCache"></param>
    /// <param name="outpath"></param>
    /// <returns></returns>
    private static Dictionary<string, string> SaveCache(Dictionary<string, string> saveCache, string outpath)
    {
        List<CacheItem> list = new List<CacheItem>();
        foreach (KeyValuePair<string, string> kv in saveCache)
        {
            var item = new CacheItem();
            item.Name = kv.Key;
            item.UIID = kv.Value;
            list.Add(item);
        }

        string json = JsonMapper.ToJson(list);
        //配置写入本地
        var configPath = IPath.Combine(outpath, "Art/CacheConfig.json");
        var direct = Path.GetDirectoryName(configPath);
        if (Directory.Exists(direct) == false)
        {
            Directory.CreateDirectory(direct);
        }

        File.WriteAllText(configPath, json);
        return saveCache;
    }


    /// <summary>
    /// 移除无效资源
    /// </summary>
    public static void RemoveAllAbName()
    {
        EditorUtility.DisplayProgressBar("资源清理", "清理AssetBundle Name", 0);

        var paths = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);
        foreach (var path in paths)
        {
            var ext = Path.GetExtension(path).ToLower();
            if (ext == ".cs" || ext == ".js" || ext == ".dll")
            {
                continue;
            }

            var _path = path.Replace("\\", "/");
            //获取被依赖的路径
            var p = "Assets" + _path.Replace(Application.dataPath, "");

            //还原ABname
            AssetImporter ai = AssetImporter.GetAtPath(p);
            if (ai == null)
            {
                continue;
            }
            
            ai.assetBundleName = null;
            EditorUtility.DisplayProgressBar("资源清理", "清理:" + p, 1);
        }

        EditorUtility.ClearProgressBar();
    }


    /// <summary>
    /// 获取文件的md5
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            file.Dispose();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return "";
        }
    }
}
