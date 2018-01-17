using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateAssetBundle_Editor
{
    private static readonly string sConfigFile = "AllAssetsConfig.xml";

    private static int m_totalFileCount = 0;



    public static string m_targetPackagePath;


    public static void Execute(UnityEditor.BuildTarget target)
    {
        PackAllResources();
        AssetDatabase.Refresh();
    }

    public static void SaveBinaryFile()
    {
        InitConfig();

        //打包成二进制数据
        string path = m_targetPackagePath + "/" + sConfigFile;
        FileStream stream = new FileInfo(path).OpenRead();
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, Convert.ToInt32(stream.Length));

        AssetDatabase.Refresh();
    }

    private static void InitConfig()
    {
        //m_configTabFileList.Clear();
        m_totalFileCount = 0;
        m_curIndex = 0;

      //  string TargetPath = AssetBundleCtrl_Windows.GetPlatformPath(AssetBundleCtrl_Windows.buildTarget);
    }

    public static void PackAllResources()
    {
        InitConfig();

        //1.获取在Resources根目录
        string rootPath = Path.Combine(Application.dataPath, AssetBundleCtrl_Windows.g_rootResourceDir);

        //2.生成目录资源配置文件
        CreateABName(rootPath);

        //3.打包
        PackageAll();
    }


    public static void CreateABName(string rootPath)
    {  
        //扫描所有文件
        var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
        var fileList = new List<string>(allFiles);
        //剔除不打包的部分
        for (int i = fileList.Count -1; i>=0;i--  )
        {
            var fi = allFiles[i];
            var extension = Path.GetExtension(fi.ToLower());
            //
            if (extension == ".meta" || extension == ".cs" || extension == ".js")
            {
                fileList.RemoveAt(i);
            }
        }

        SineAllResABName(fileList.ToArray());
        //
        AssetDatabase.Refresh();
    }


    private static void PackageAll()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();

        var inAssetPath = AssetBundleCtrl_Windows.GetPlatformPath(AssetBundleCtrl_Windows.buildTarget);
        string buildDir = Path.Combine(Application.streamingAssetsPath, inAssetPath);
        if(Directory.Exists(buildDir) == false)
        {
            Directory.CreateDirectory(buildDir);
        }

        //使用lz4压缩
        BuildPipeline.BuildAssetBundles(buildDir, BuildAssetBundleOptions.ChunkBasedCompression, AssetBundleCtrl_Windows.buildTarget);

        EditorUtility.ClearProgressBar();
    }


    private static int m_curIndex = 0;
    private static void SineAllResABName(string[] paths)
    {
        foreach (var path in paths)
        {

            var _path = path.Replace("\\", "/");
            float val = m_curIndex * 1.0f / paths.Length;
            EditorUtility.DisplayProgressBar("Updating", "Packaging, " + Path.GetFileNameWithoutExtension(_path) + "please wait..." + val * 100 + "%", val);
            m_curIndex++;
            var dependsource = "Assets" + _path.Replace(Application.dataPath, "");
             BDeBug.I.Log("-----------------------------------------");
             BDeBug.I.Log("source:" + dependsource);
             BDeBug.I.Log("path:" + _path);

            string[] allDependObjectPaths = AssetDatabase.GetDependencies(dependsource);
            for (int i = 0; i < allDependObjectPaths.Length; i++)
            {
                var dependPath = allDependObjectPaths[i];
                 BDeBug.I.Log("depend on:" + dependPath);

                AssetImporter ai = AssetImporter.GetAtPath(dependPath);
                if (ai == null)
                {
                    BDeBug.I.Log("not find Resource " + dependPath);
                    continue;
                }

                //重新组建ab名字，带上路径名
                 dependPath = Path.GetFullPath(dependPath);
                 dependPath = dependPath.Replace("\\", "/");
                 var temp = dependPath.Split('/');
                 string derictory="";
                 bool isAdd = false;
                 foreach (var s in temp)
                 {
                     if (isAdd)
                     {
                         if (derictory == "")
                         {
                             derictory = s;
                         }
                         else
                         {
                             derictory = derictory + "-" + s;
                         }
                        
                     }
                     else if (s.Equals("Resources") || s.Equals("resources"))
                     {
                         isAdd = true;
                     }
                 }
                 //不在resource内部
                if(isAdd ==  false)
                {
                    foreach (var s in temp)
                    {
                        if (isAdd)
                        {
                            if (derictory == "")
                            {
                                derictory = s;
                            }
                            else
                            {
                                derictory = derictory + "-" + s;
                            }

                        }
                        else if (s.Equals("ArtRes"))
                        {
                            isAdd = true;
                        }
                    }
                }
                derictory = derictory.Replace(".", "+");
                ai.assetBundleName = derictory;
                ai.assetBundleVariant = "";
            }
        }
    }
}

