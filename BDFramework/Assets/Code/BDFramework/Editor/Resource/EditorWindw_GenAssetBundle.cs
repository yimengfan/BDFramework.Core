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
    public string rootResourceDir = "Resource/Runtime/";

    /// <summary>
    /// 
    /// </summary>
    public static UnityEditor.BuildTarget buildTarget = BuildTarget.StandaloneWindows;

    private int @select = 0;

    void DrawToolsBar()
    {
        @select = GUILayout.Toolbar(@select, new string[] {"Windows32", "iOS/Android"});
     
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        TipsGUI();
        DrawToolsBar();
        GUILayout.Space(10);
        LastestGUI();
        GUILayout.EndVertical();
    }


    /// <summary>
    /// 最新包
    /// </summary>
    void LastestGUI()
    {
        GUILayout.Label("最新包[对比增量下载]：");


        GUILayout.BeginHorizontal();
        GUILayout.Space(80);
        if (GUILayout.Button("一键打包[美术资源]", GUILayout.Width(300), GUILayout.Height(30)))
        {
            //开始打包
            if (@select == 0)
            {
                AssetBundleEditorTools.Start(rootResourceDir, BuildTarget.StandaloneWindows);
            }
            else
            {
                AssetBundleEditorTools.Start(rootResourceDir, BuildTarget.Android);
                AssetBundleEditorTools.Start(rootResourceDir, BuildTarget.iOS);
            }
        }

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        {
            GUILayout.Space(80);
            if (GUILayout.Button("上传CDN[Test]", GUILayout.Width(300), GUILayout.Height(30)))
            {
                
            }
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 提示UI
    /// </summary>
    void TipsGUI()
    {
        GUILayout.Label(string.Format("资源根目录:Assets/{0}", rootResourceDir));
        GUILayout.Label(string.Format("AB输出目录:StreammingAssets/{0}", rootResourceDir));
    }
}