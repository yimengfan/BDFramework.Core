using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

static public class AssetBundleEditorTools
{
    public static void Start(string resRootPath, BuildTarget target)
    {
        //1.生成ab名
        string rootPath = Path.Combine(Application.dataPath, resRootPath);
        CreateAbName(rootPath);
        //2.打包
        BuildAssetBundle(target);
    }


    /// <summary>
    /// 创建ab名
    /// </summary>
    /// <param name="rootPath"></param>
    public static void CreateAbName(string rootPath)
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

        AnalyzeResource(fileList.ToArray());
        //
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 创建assetbundle
    /// </summary>
    private static void BuildAssetBundle(BuildTarget target)
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        string saveDir = "";
        switch (target)
        {
            case BuildTarget.Android:
                saveDir = "Android";
                break;
            case BuildTarget.iOS:
                saveDir = "iOS";
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                saveDir = "Windows";
                break;
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
                saveDir = "OSX";
                break;
            default:
                break;
        }

        //
        if (saveDir == "")
        {
            return;
        }

        saveDir = Path.Combine(Application.streamingAssetsPath,saveDir+"/Art");
        if (Directory.Exists(saveDir) == false)
        {
            Directory.CreateDirectory(saveDir);
        }

        //使用lz4压缩
        BuildPipeline.BuildAssetBundles(saveDir, BuildAssetBundleOptions.ChunkBasedCompression,target);
        EditorUtility.ClearProgressBar();
    }


   

    private static void AnalyzeResource(string[] paths)
    {
        float curIndex = 0;
        foreach (var path in paths)
        {
            var _path = path.Replace("\\", "/");
            float val = curIndex / paths.Length;
            EditorUtility.DisplayProgressBar("分析资源","执行..., " + Path.GetFileNameWithoutExtension(_path) + "please wait..." + val * 100 + "%", val);
            curIndex++;
            //获取被依赖的路径
            var dependsource = "Assets" + _path.Replace(Application.dataPath, "");
            
            BDebug.Log("分析:" + dependsource);
            string[] allDependObjectPaths = AssetDatabase.GetDependencies(dependsource);
            for (int i = 0; i < allDependObjectPaths.Length; i++)
            {
                var dependPath = allDependObjectPaths[i];
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