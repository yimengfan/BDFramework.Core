using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

public class EditorWindw_GenAssetBundle : EditorWindow
{
    /// <summary>
    /// 资源下面根节点
    /// </summary>
    public static string g_rootResourceDir = "ArtRes/Resources/";

    /// <summary>
    /// 
    /// </summary>
    public static UnityEditor.BuildTarget buildTarget = BuildTarget.StandaloneWindows;
	private int m_curSelect = 2;
    public static bool encodeLuaFile = false;

    private Editor_GenLocalDataPacket mEditorVersion;

	void DrawToolsBar()
	{
		m_curSelect = GUILayout.Toolbar(m_curSelect,new string[]{"Windows32","iOS","Android"});
		if(m_curSelect == 0)
			buildTarget	= BuildTarget.StandaloneWindows;
		else if(m_curSelect == 1)
			buildTarget	= BuildTarget.iOS;
		else if(m_curSelect == 2)
			buildTarget	= BuildTarget.Android;
	}


   new public void Show() {

        base.Show();
        switch(EditorUserBuildSettings.activeBuildTarget){
            case BuildTarget.StandaloneWindows64 :
            case BuildTarget.StandaloneWindows:
                m_curSelect = 0;
                break;
            case BuildTarget.iOS:
                m_curSelect = 1;
                break;
            case BuildTarget.Android:
                m_curSelect = 2;
                break;
        
        }
    }


   bool isSelectAll = false;
	void OnGUI()
	{
        GUILayout.BeginVertical();
        TipsGUI();
	    DrawToolsBar();
        GUILayout.Space(10);
        IncrementalGUI();
        LastestGUI();
        GUILayout.EndVertical();
	}


    public void Update() {

        CreateHashList.Update(buildTarget);
    }

	
	public static string GetPlatformPath(UnityEditor.BuildTarget target)
	{
		string SavePath = "";
		switch (target)
		{
		case BuildTarget.StandaloneWindows:
			SavePath = "AssetBundle/Windows32/";
			break;
		case BuildTarget.StandaloneWindows64:
			SavePath = "AssetBundle/Windows64/";
			break;
		case BuildTarget.iOS:
			SavePath = "AssetBundle/IOS/";
			break;
		//case BuildTarget.StandaloneOSX:
		//	SavePath = "AssetBundle/Mac/";
			//break;
		case BuildTarget.Android:
			SavePath = "AssetBundle/Android/";
			break;
		default:
			SavePath = "AssetBundle/";
			break;
		}
		
		if (Directory.Exists(SavePath) == false)
			Directory.CreateDirectory(SavePath);
		
		return SavePath;
	}
	
	public static string GetPlatformName(UnityEditor.BuildTarget target)
	{
		string platform = "Windows32/AllResources/";
		switch (target)
		{
		    case BuildTarget.StandaloneWindows:
			    platform = "Windows32/AllResources/";
			    break;
		    case BuildTarget.StandaloneWindows64:
			    platform = "Windows64/AllResources/";
			    break;
		    case BuildTarget.iOS:
			    platform = "IOS/AllResources/";
			    break;
		    //case BuildTarget.StandaloneOSX:
			   // platform = "Mac/AllResources/";
			   // break;
		    case BuildTarget.Android:
			    platform = "Android/AllResources/";
			    break;
		    default:
			    break;
		}
		return platform;
	}


    Queue<Action> mTaskQue = new Queue<Action>();
    void IncrementalGUI()
    {
        //增量包按钮
        GUILayout.Label("增量包：");


        GUILayout.BeginHorizontal();
        GUILayout.Space(80);
        if (GUILayout.Button("一键打包: [增量]资源包", GUILayout.Width(300), GUILayout.Height(30)))
        {

            //开始打包
            ECreateAssetBundle.Execute(buildTarget);
            //生成hash
            CreateHashList.StartLoad((bool issuccess) =>
            {
                EditorUtility.DisplayDialog("", "打包成功", "OK");
            });
        }
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.Space(80);
        if (GUILayout.Button("生成服务端增量文件", GUILayout.Width(300), GUILayout.Height(30)))
        {
            //mEditorVersion = new Editor_VersionCtrl();
            //mEditorVersion.mResServerAddress = "http://api-resource.ptdev.cn/v1/res";
            ////获取上个版本的hash
            //string platform = AssetBundleCtrl_Windows.GetPlatformName(buildTarget);
            //string newVersionHash = System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform + "/VersionNum/VersionHash-old.xml");
            ////将服务器的hash写到本地
            //mEditorVersion.Start("3", "100", "V0uFhE2GRNnRipS0hery9OhY", newVersionHash, (bool issuccess) =>
            //{
            //    if (issuccess)
            //    {
            //        mTaskQue.Enqueue(() =>
            //            {
            //                CreateVersionUpdateList.Execute(buildTarget);
            //                EditorUtility.DisplayDialog("", "生成成功", "OK");
            //            });

            //    }
            //    else
            //    {
            //        mTaskQue.Enqueue(() =>
            //        {
            //            EditorUtility.DisplayDialog("", "生成失败", "OK");
            //        });

            //    }
            //});
          
        }
        if (mTaskQue.Count > 0)
        {
            var action = mTaskQue.Dequeue();
            action();
        }
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        GUILayout.Space(80);
        if (GUILayout.Button("一键导出", GUILayout.Width(300), GUILayout.Height(30)))
        {

            var outpath = EditorUtility.OpenFolderPanel("选择导出文件夹", "", "");
            if (outpath != null && outpath!="")
            {
                CreateVersionUpdateList.ExportFile(buildTarget, outpath);
                EditorUtility.DisplayDialog("",buildTarget.ToString()+ "- 导出到"+outpath+"成功", "OK");
            }
        }
        GUILayout.EndHorizontal();
    }


    void LastestGUI()
    {

        //最新包按钮
        GUILayout.Label("最新包：");
        GUILayout.BeginHorizontal();

        GUILayout.Space(80);
        if (GUILayout.Button("一键打包: [最新]资源包", GUILayout.Width(300), GUILayout.Height(25)))
        {
            EditorUtility.DisplayDialog("", "功能尚未支持", "OK");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(80);
        if (GUILayout.Button("一键导出", GUILayout.Width(300), GUILayout.Height(25)))
        {

            EditorUtility.DisplayDialog("", "功能尚未支持", "OK");
        }
        GUILayout.EndHorizontal();

    }

    void TipsGUI()
    {

        GUILayout.Label(string.Format("资源根目录:Asset/{0}", g_rootResourceDir));
        GUILayout.Label(string.Format("AB输出目录:StreammingAssets/{0}", g_rootResourceDir));
    }
}
