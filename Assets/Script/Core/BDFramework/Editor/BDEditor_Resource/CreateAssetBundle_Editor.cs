using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
//using System.Xml.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

using UnityEngine;
using UnityEditor;

public class CreateAssetBundle_Editor
{
    private static readonly string sPackConfigFileDir = "PackResourceFile";
    private static readonly string sConfigFile = "AllAssetsConfig.xml";
    private static readonly string sConfigPackFile = "AllAssetsConfig.bytes";
    private static readonly string sFirstSceneName = "Startup";
    private static readonly string sSceneFileFullName = "LogicScene.bytes";

    //private static readonly string sActionFileDir = "/AllResources/Action/Resources/";

    private static SortedList<string, TabFile> m_configTabFileList = new SortedList<string, TabFile>();
    private static int m_totalFileCount = 0;
    private static int m_curIndex = 0;

    private static List<string> m_configFileList = new List<string>();

    public static string m_targetPackagePath;

    private static int m_configFileCount = 0;

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
        string targetPath = m_targetPackagePath + "/" + sConfigPackFile;
        File.WriteAllBytes(targetPath, buffer);

        AssetDatabase.Refresh();
    }

    private static void InitConfig()
    {
        m_configFileList.Clear();
        //m_configTabFileList.Clear();
        m_totalFileCount = 0;
        m_curIndex = 0;

        string TargetPath = AssetBundleCtrl_Windows.GetPlatformPath(AssetBundleCtrl_Windows.buildTarget);
        m_targetPackagePath = PathUtility.CombinePath(Application.dataPath, TargetPath.Substring(7));
        PathUtility.CreateDirInPath(m_targetPackagePath);
    }

    public static void PackAllResources()
    {
        InitConfig();

        //1.获取在AllResources目录下的所有Resources目录
        List<string> ResourcesDirList = ListResourcesDirInAllResources();
        if (ResourcesDirList.Count == 0)
        {
            UnityEngine.Debug.LogError("没有找到任何Resources目录");
            return;
        }

        //2.生成目录资源配置文件
        CreateConfig(ResourcesDirList);

        //3.统计打包资源的量
        CalcNeedPackageFileCount();

        //4.打包
        PackageAll();
    }


    //列出在AllResources目录下所有Resources目录
    public static List<string> ListResourcesDirInAllResources()
    {
        string rootPath = PathUtility.CombinePath(Application.dataPath, AssetBundleCtrl_Windows.g_rootResourceDir);
        List<string> pathList = new List<string>();

        if (!string.IsNullOrEmpty(rootPath))
            Directory.CreateDirectory(rootPath);

        PathUtility.GetAllDirInDir(rootPath, "Resources",pathList);

        return pathList;
    }


    public static void CreateConfig(List<string> pathList)
    {
        m_configFileCount = 0;
        string configFileDir = Application.dataPath + "/" + sPackConfigFileDir;
        if (!Directory.Exists(configFileDir))
        {
            Directory.CreateDirectory(configFileDir);
        }

        for (int i = 0; i < pathList.Count; i++)
        {
            string onePath = PathUtility.CombinePath(pathList[i], "Resources");
            DirectoryInfo info = new DirectoryInfo(onePath);
            int count = 1;
            string finalStr = "id\tFilePath\r\n";
            //扫描所有文件
            var allFiles = info.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var fi in allFiles )
            {
                if (Path.GetExtension(fi.Name.ToLower()) == ".meta")
                    continue;

                string ResourcePath = fi.FullName.Substring(fi.FullName.IndexOf("ArtRes"));
                string temp = ResConfigFile.ContentHelpWrite(count++, ResourcePath);
                finalStr += temp;
            }


            string configFileName = info.Parent.Name + m_configFileCount.ToString() + ".bytes";
            ResConfigFile.GenFile(configFileDir + "/" + configFileName, finalStr);
            m_configFileList.Add(configFileName);
            m_configFileCount++;
        }

        GenSceneConfig();

        AssetDatabase.Refresh();
    }

    private static void GenSceneConfig()
    {
        string configFileDir = Application.dataPath + "/" + sPackConfigFileDir;
        string allcontent = "id\tFilePath\r\n"; ;
        int count = 1;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            string pathName = scene.path;
            string FirstScene = Path.GetFileNameWithoutExtension(pathName);
            if (sFirstSceneName == FirstScene)
                continue;
            if (!scene.enabled)
                continue;

            pathName = pathName.Substring(7);
            allcontent += ResConfigFile.ContentHelpWrite(count, pathName);
            count++;
        }
        ResConfigFile.GenFile(configFileDir + "/" + sSceneFileFullName, allcontent);

        m_configFileList.Add(sSceneFileFullName);
    }

    private static void PackageAll()
    {
        ////先打包Other部分，再打包Scene部分
        foreach (KeyValuePair<string, TabFile> pair in m_configTabFileList)
        {
            PackageOnePath(pair.Key, pair.Value);
        }
        AssetDatabase.RemoveUnusedAssetBundleNames();

        string buildDir = CreateAssetBundle_Editor.m_targetPackagePath;

        BuildPipeline.BuildAssetBundles(buildDir, BuildAssetBundleOptions.None, AssetBundleCtrl_Windows.buildTarget);


        EditorUtility.ClearProgressBar();
    }
    private static void CalcNeedPackageFileCount()
    {
        for (int i = 0; i < m_configFileList.Count; i++)
        {
            string strPath = m_configFileList[i];
            string fileDir = PathUtility.CombinePath("Assets", sPackConfigFileDir);
            fileDir = PathUtility.CombinePath(fileDir, strPath);
            TextAsset textAsset = AssetDatabase.LoadMainAssetAtPath(fileDir) as TextAsset;
            if (textAsset == null)
            {
                UnityEngine.Debug.Log(string.Format("not find {0}", strPath));
                return;
            }

            //string AssetType = Path.GetFileNameWithoutExtension (strPath);
            fileDir = PathUtility.CombinePath(sPackConfigFileDir, strPath);
            string FileFullName = PathUtility.CombinePath(Application.dataPath, fileDir);
            StreamReader sr = new StreamReader(FileFullName);
            string totalStr = sr.ReadToEnd();
            if (string.IsNullOrEmpty(totalStr))
                return;

            TabFile tf = new TabFile(FileFullName, totalStr);

            if (m_configTabFileList.ContainsKey(strPath))
            {
                UnityEngine.Debug.Log("Find Same Config File " + strPath);
                continue;
            }
            m_configTabFileList.Add(strPath, tf);

            m_totalFileCount += tf.GetCount();
        }

    }

    private static void PackageOnePath(string configFilePath, TabFile tf)
    {
        while (tf.Next())
        {
            string strResFilePath = tf.Get<string>("FilePath");

            float val = m_curIndex * 1.0f / m_totalFileCount;
            EditorUtility.DisplayProgressBar("Updating", "Packaging, " + Path.GetFileNameWithoutExtension(strResFilePath) + "please wait..." + val * 100 + "%", val);
            m_curIndex++;


            string realFilePath = "Assets" + "/" + strResFilePath;
            //sPackageManager.AddAsset(realFilePath,AssetType);
            realFilePath = realFilePath.Replace("\\", "/");

            string[] allDependObjectPaths = AssetDatabase.GetDependencies(new string[] { realFilePath });
            for (int i = 0; i < allDependObjectPaths.Length; i++)
            {
                string Ext = Path.GetExtension(allDependObjectPaths[i]).ToLower();
                if (Ext == ".cs" || Ext == ".js")
                    continue;
                AssetImporter ai = AssetImporter.GetAtPath(allDependObjectPaths[i]);
                if (ai == null)
                {
                    UnityEngine.Debug.Log("not find Resource " + allDependObjectPaths[i]);
                    continue;
                }

                //替换符号
                allDependObjectPaths[i] = allDependObjectPaths[i].Replace("\\", "/");
                var temp = allDependObjectPaths[i].Split('/');
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
                             derictory = derictory + "_" + s;
                         }
                        
                     }
                     else if (s.Equals("Resources") || s.Equals("Resources"))
                     {
                         isAdd = true;
                     }
                 }
                derictory = derictory.Replace(".", "@");
                ai.assetBundleName = derictory;
                ai.assetBundleVariant = "";
            }
        }
    }

    static string curPath;
    static string strTgtPath;
    static string finalPath;
    static UnityEngine.Object mSelfObj;
    static string md5Value;
}

