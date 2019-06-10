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
using UnityEngine.Networking.NetworkSystem;


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
 

        
        //1.生成ab名
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

        //配置写入本地
        var configPath = IPath.Combine(outPath, "Art/Config.json");

        //2.打包
        //更新的部分放在另外一个路径
        //然后再合并
        var newpath = outPath;
        if (File.Exists(configPath))
        {
            newpath = outPath + "/Art_new";
            if (Directory.Exists(newpath))
            {
                Directory.Delete(newpath);
            }

            Directory.CreateDirectory(newpath);
        }

        //3.生成AssetBundle
        BuildAssetBundle(target, newpath, options);

        //保存last.json
        if (File.Exists(configPath))
        {
            var lastPath = IPath.Combine(outPath, "Art/Config_last.json");
            File.Copy(configPath, lastPath, true);
        }

        var direct = Path.GetDirectoryName(configPath);
        if (Directory.Exists(direct) == false)
        {
            Directory.CreateDirectory(direct);
        }

        File.WriteAllText(configPath, curManifestConfig.ToString());

        //合并
//        if (newpath != outPath)
//        {
//            var newFs = Directory.GetFiles(newpath, "*.*", SearchOption.AllDirectories);
//
//            foreach (var nf in newFs)
//            {
//                var filename = nf.Replace(newpath, "");
//                var copyto = IPath.Combine(outPath, filename);
//                File.Copy(nf,copyto,true);
//                Debug.Log("合并新文件:" + nf);
//            }
//        }

        //  Directory.Delete(newpath,true);

        
        //4.清除AB Name
        RemoveAllAbName();
        
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

    static ManifestConfig curManifestConfig = null;


    /// <summary>
    /// 分析资源
    /// </summary>
    /// <param name="paths"></param>
    /// <param name="target"></param>
    /// <param name="outpath"></param>
    private static void AnalyzeResource(string[] paths, BuildTarget target, string outpath)
    {
        var lastConfigPath = IPath.Combine(outpath, "Art/Config.json");
        if (File.Exists(lastConfigPath))
        {
            curManifestConfig = new ManifestConfig(File.ReadAllText(lastConfigPath));
        }
        else
        {
            curManifestConfig = new ManifestConfig();
        }

        List<string> changeList = new List<string>();
        float curIndex = 0;

        foreach (var path in paths)
        {
            var _path = path.Replace("\\", "/");

            EditorUtility.DisplayProgressBar("分析资源 -" + target,
                "分析:" + Path.GetFileNameWithoutExtension(_path) + "   进度：" + curIndex + "/" + paths.Length,
                curIndex / paths.Length);
            curIndex++;


            var UIID = GetMD5HashFromFile(_path);
            if (string.IsNullOrEmpty(UIID))
            {
                continue;
            }
            
            //获取被依赖的路径
            var dependsource = "Assets" + _path.Replace(Application.dataPath, "");
            var allDependObjectPaths = AssetDatabase.GetDependencies(dependsource).ToList();
            dependsource = dependsource.ToLower();
            //
            ManifestItem lastItem = null;
            curManifestConfig.Manifest.TryGetValue(dependsource, out lastItem);
            //last没有或者 uiid不一致被改动,
            //TODO 有改动的要把依赖重新打
            if (lastItem != null && lastItem.UIID == UIID)
            {
                continue;
            }
            else
            {
                Debug.Log("打包:" +dependsource);
            }
            List<string> dependAssets = new List<string>();
            //处理依赖资源打包
            for (int i = 0; i < allDependObjectPaths.Count; i++)
            {
                //
                var dependPath = allDependObjectPaths[i];
                //脚本不打包
                var ext = Path.GetExtension(dependPath).ToLower();
                if (ext == ".cs" || ext == ".js" || ext == ".dll")
                {
                    continue;
                }
                //
                AssetImporter ai = AssetImporter.GetAtPath(dependPath);
                if (ai == null)
                {
                    BDebug.Log("资源不存在:" + dependPath);
                    continue;
                }
                
                var dependObjPath = Application.dataPath + dependPath.TrimStart("Assets".ToCharArray());

                var uiid = GetMD5HashFromFile(dependObjPath);
                if (string.IsNullOrEmpty(uiid))
                {
                    continue;
                }

                string abname = "assets" + dependObjPath.Replace(Application.dataPath, "").ToLower();
                
                ai.assetBundleName = abname;
                ai.assetBundleVariant = "";
                
                //被依赖的文件,不保存其依赖信息
                if (abname != dependsource) //依赖列表中会包含自己
                {
                    curManifestConfig.AddDepend(abname, uiid, new List<string>());
                }

                dependAssets.Add(abname);
            }

            changeList.Add(dependsource);
            //保存主文件的依赖
            if (dependAssets.Count > 0)
            {
                dependAssets.Remove(dependsource);
                curManifestConfig.AddDepend(dependsource, UIID, dependAssets);
            }
        }


        EditorUtility.ClearProgressBar();
        Debug.Log("本地需要打包资源:" + changeList.Count);
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