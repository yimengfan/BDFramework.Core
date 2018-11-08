using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFramework.ResourceMgr;
using UnityEngine;
using UnityEditor;

static public class AssetBundleEditorTools
{
    public static void GenAssetBundle(string resRootPath, string outPath, BuildTarget target)
    {
        //1.生成ab名
        string rootPath = Path.Combine(Application.dataPath, resRootPath);
        CreateAbName(rootPath, target, outPath);
        //2.打包
        BuildAssetBundle(target, outPath);
    }


    /// <summary>
    /// 创建ab名
    /// </summary>
    /// <param name="rootPath"></param>
    public static void CreateAbName(string rootPath, BuildTarget target, string outpath)
    {
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

        AnalyzeResource(fileList.ToArray(), target, outpath);
    }

    /// <summary>
    /// 创建assetbundle
    /// </summary>
    private static void BuildAssetBundle(BuildTarget target, string outPath)
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        string path = Path.Combine(outPath, "Art");
        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
        }

        //使用lz4压缩
        BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.ChunkBasedCompression, target);
        EditorUtility.ClearProgressBar();
    }


    private static void AnalyzeResource(string[] paths, BuildTarget target, string outpath)
    {
        ManifestConfig manifestConfig = null;
        var configPath = Path.Combine(outpath, "Art/Config.json");
        if (File.Exists(configPath))
        {
            var content = File.ReadAllText(configPath);
            manifestConfig = new ManifestConfig(content);
        }
        else
        {
            manifestConfig = new ManifestConfig();
        }

        int counter = 0;
        float curIndex = 0;
        foreach (var path in paths)
        {
            var _path = path.Replace("\\", "/");

            EditorUtility.DisplayProgressBar("分析资源 -" + target.ToString(),
                "分析:" + Path.GetFileNameWithoutExtension(_path) + "   进度：" + curIndex + "/" + paths.Length,
                curIndex / paths.Length);
            curIndex++;
            //获取被依赖的路径
            var dependsource = "Assets" + _path.Replace(Application.dataPath, "");
            var allDependObjectPaths = AssetDatabase.GetDependencies(dependsource).ToList();

            var manifestItem = manifestConfig.GetManifestItem(dependsource.ToLower());
            var Uiid = GetMD5HashFromFile(_path);
            // 
            var isEquals = manifestItem != null && Uiid == manifestItem.UIID;
            List<string> newAssets = new List<string>();
            //处理依赖资源是否打包
            for (int i = 0; i < allDependObjectPaths.Count; i++)
            {
                //
                var dependPath = allDependObjectPaths[i];
                var ext = Path.GetExtension(dependPath).ToLower();
                if (ext == ".cs" || ext == ".js")
                {
                    continue;
                }

                //
                AssetImporter ai = AssetImporter.GetAtPath(dependPath);
                if (ai == null)
                {
                    BDebug.Log("not find Resource " + dependPath);
                    continue;
                }


                //重新组建ab名字，带上路径名
                dependPath = Path.GetFullPath(dependPath);
                dependPath = dependPath.Replace("\\", "/");
                //根据是否相等,判断是否打包
                if (isEquals)
                {
                    //本次不打包
                    ai.assetBundleName = null;
                }
                else
                {
                    //本次打包
                    string derictory = "assets" + dependPath.Replace(Application.dataPath, "");
                    ai.assetBundleName = derictory.ToLower();

                    newAssets.Add(ai.assetBundleName);
                    ai.assetBundleVariant = "";
                }
            }


            //将现在的目录结构替换配置中的
            if (newAssets.Count > 0)
            {
                manifestConfig.AddDepend(dependsource.ToLower(), Uiid, newAssets);
                counter++;
            }
        }

        Debug.Log("本地需要打包资源:" + counter);
        var direct = Path.GetDirectoryName(configPath);
        if (Directory.Exists(direct) == false)
        {
            Directory.CreateDirectory(direct);
        }
        //写入本地
        File.WriteAllText(configPath, manifestConfig.ToString());
    }


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
            throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
        }
    }
}