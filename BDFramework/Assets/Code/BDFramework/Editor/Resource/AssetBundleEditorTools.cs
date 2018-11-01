using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

static public class AssetBundleEditorTools
{
    public static void GenAssetBundle(string resRootPath,string outPath, BuildTarget target )
    {
        //1.生成ab名
        string rootPath = Path.Combine(Application.dataPath, resRootPath);
        CreateAbName(rootPath,target);
        //2.打包
        BuildAssetBundle(target,outPath);
    }


    /// <summary>
    /// 创建ab名
    /// </summary>
    /// <param name="rootPath"></param>
    public static void CreateAbName(string rootPath,BuildTarget target)
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

        AnalyzeResource(fileList.ToArray(),target);
        //
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 创建assetbundle
    /// </summary>
    private static void BuildAssetBundle(BuildTarget target ,string outPath)
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        string platform  = Path.Combine(outPath,"Art");
        if (Directory.Exists(platform) == false)
        {
            Directory.CreateDirectory(platform);
        }

        //使用lz4压缩
        BuildPipeline.BuildAssetBundles(platform, BuildAssetBundleOptions.ChunkBasedCompression,target);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }


   

    private static void AnalyzeResource(string[] paths,BuildTarget target)
    {
        float curIndex = 0;
        foreach (var path in paths)
        {
            var _path = path.Replace("\\", "/");
                       
            EditorUtility.DisplayProgressBar("分析资源 -" +target.ToString(),
                "打包:" + Path.GetFileNameWithoutExtension(_path) +"   进度：" +  curIndex +"/" +paths.Length,  curIndex / paths.Length);
            curIndex++;
            //获取被依赖的路径
            var dependsource = "Assets" + _path.Replace(Application.dataPath, "");
            
            BDebug.Log("分析:" + dependsource);
            string[] allDependObjectPaths = AssetDatabase.GetDependencies(dependsource);
            for (int i = 0; i < allDependObjectPaths.Length; i++)
            {
                var dependPath = allDependObjectPaths[i];
                var ext = Path.GetExtension(dependPath).ToLower();
                //默认不打包cs代码
                if (ext!= ".cs")
                {
                    BDebug.Log("depend on:" + dependPath);

                    AssetImporter ai = AssetImporter.GetAtPath(dependPath);
                    if (ai == null)
                    {
                        BDebug.Log("not find Resource " + dependPath);
                        continue;
                    }

                    //重新组建ab名字，带上路径名
                    dependPath = Path.GetFullPath(dependPath);
                    dependPath = dependPath.Replace("\\", "/");
                    string derictory = "assets"+ dependPath.Replace(Application.dataPath,"");
                    ai.assetBundleName = derictory.ToLower();
                    ai.assetBundleVariant = "";
                }
               
            }
        }
    }
}