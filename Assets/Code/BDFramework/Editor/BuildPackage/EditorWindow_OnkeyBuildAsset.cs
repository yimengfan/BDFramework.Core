using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Helper;
using BDFramework.Editor;
using BDFramework.Editor.BuildPackage;
using UnityEditor;
using UnityEngine;

public class EditorWindow_OnkeyBuildAsset : EditorWindow
{
    private EditorWindow_Table editorTable;

    private EditorWindow_ScriptBuildDll editorScript;
    private EditorWindow_GenAssetBundle editorAsset;

    public void Show()
    {
        this.editorTable = new EditorWindow_Table();
        this.editorAsset = new EditorWindow_GenAssetBundle();
        this.editorScript = new EditorWindow_ScriptBuildDll();

        this.minSize = this.maxSize = new Vector2(1050, 600);
        base.Show();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        {
            if (editorScript != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                editorScript.OnGUI();
                GUILayout.EndVertical();
                Layout_DrawLineV(Color.white);
            }

            if (editorAsset != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                editorAsset.OnGUI();
                GUILayout.EndVertical();
                Layout_DrawLineV(Color.white);
            }

            if (editorTable != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                editorTable.OnGUI();
                GUILayout.EndVertical();
                Layout_DrawLineV(Color.white);
            }
        }
        GUILayout.EndHorizontal();

        Layout_DrawLineH(Color.white);
        OnGUI_OneKeyExprot();
    }


    public string exportPath = "";
    private bool isGenIosAssets = false;
    private bool isGenAndroidAssets = true;

    public void OnGUI_OneKeyExprot()
    {
        GUILayout.BeginVertical();
        {
            GUILayout.Label("注:上面按钮操作,会默认生成到StreamingAssets", GUILayout.Width(500), GUILayout.Height(30));
            isGenAndroidAssets = GUILayout.Toggle(isGenAndroidAssets, "生成Android资源(Windows公用)");
            isGenIosAssets = GUILayout.Toggle(isGenIosAssets, "生成Ios资源");

            //
            GUILayout.Label("导出地址:" + exportPath, GUILayout.Width(500));
            //
            if (GUILayout.Button("一键导出[自动转hash]", GUILayout.Width(350), GUILayout.Height(30)))
            {
                //选择目录
                exportPath = EditorUtility.OpenFolderPanel("选择导出目录", exportPath, "");
                if (string.IsNullOrEmpty(exportPath))
                {
                    return;
                }

                //开始生成资源
                {
                    //生成android资源
                    if (isGenAndroidAssets)
                    {
                        var outPath = exportPath + "/" + BDUtils.GetPlatformPath(RuntimePlatform.Android);
                        //1.编译脚本
                        try
                        {
                            ScriptBuildTools.BuildDll(Application.dataPath, outPath);
                            EditorWindow_ScriptBuildDll.GenCLRBindingByAnalysis();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            return;
                        }
                        //2.打包资源
                        try
                        {
                            AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/", outPath,
                                BuildTarget.Android);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            return;
                        }

                        //3.打包表格
                        try
                        {
                            Excel2SQLiteTools.GenSQLite(outPath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            return;
                        }
                    }

                    //生成ios资源
                    if (isGenIosAssets)
                    {
                        var outPath = exportPath + "/" + BDUtils.GetPlatformPath(RuntimePlatform.IPhonePlayer);
                        //1.编译脚本
                        try
                        {
                            ScriptBuildTools.BuildDll(Application.dataPath, outPath);
                            EditorWindow_ScriptBuildDll.GenCLRBindingByAnalysis();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            return;
                        }
                        //2.打包资源
                        try
                        {
                            AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/", outPath,BuildTarget.iOS);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            return;
                        }

                        //3.打包表格
                        try
                        {
                            Excel2SQLiteTools.GenSQLite(outPath);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            return;
                        }
                    }
                }
                
                EditorUtility.DisplayDialog("提示", "资源导出完成", "OK");
            }

            //
            if (GUILayout.Button("上传到文件服务器[内网测试]", GUILayout.Width(350), GUILayout.Height(30)))
            {
                //先不实现,等待使用者实现
            }

            if (GUILayout.Button("热更资源转hash[备用]", GUILayout.Width(350), GUILayout.Height(30)))
            {
                
                //选择目录
                exportPath = EditorUtility.OpenFolderPanel("选择导出目录", exportPath, "");
                if (string.IsNullOrEmpty(exportPath))
                {
                    return;
                }
                //自动转hash
                AssetUploadToServer.Assets2Hash(exportPath, "");
            }
        }
        GUILayout.EndVertical();
    }


    /// <summary>
    /// 一键build所有资源
    /// </summary>
    public static void OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform platform, string outpath)
    {
        var outPath = outpath + "/" + BDUtils.GetPlatformPath(platform);
        //1.编译脚本
        ScriptBuildTools.BuildDll(Application.dataPath, outPath);
        EditorWindow_ScriptBuildDll.GenCLRBindingByAnalysis(platform);
        //2.打包资源
        if (platform == RuntimePlatform.IPhonePlayer)
        {
            AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/", outPath,BuildTarget.iOS);
        }
        else if (platform == RuntimePlatform.Android)
        {
            AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/", outPath,BuildTarget.Android);
        }
        //3.打包表格
        Excel2SQLiteTools.GenSQLite(outPath);
    }
    
    public static void Layout_DrawLineH(Color color, float height = 4f)
    {
        Rect rect = GUILayoutUtility.GetLastRect();
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, height), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
        GUILayout.Space(height);
    }

    public static void Layout_DrawLineV(Color color, float width = 4f)
    {
        Rect rect = GUILayoutUtility.GetLastRect();
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, width, rect.height), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
        GUILayout.Space(width);
    }
}