using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BDEditorMenu
{

    [MenuItem("BDFrameWork工具箱/资源打包/DLL打包",false , 1)]
    public static void ExecuteBuildDLL()
    {
        var window = (ScriptBuildDll_Windows)EditorWindow.GetWindow(typeof(ScriptBuildDll_Windows), false, "DLL打包工具");
        window.Show();
    }

    [MenuItem("BDFrameWork工具箱/资源打包/AssetBundle打包" ,false , 2)]
    public static void ExecuteAssetBundle()
    {
        var window = (EditorWindw_GenAssetBundle)EditorWindow.GetWindow(typeof(EditorWindw_GenAssetBundle),false,"AB打包工具");
        window.Show();
    }

    [MenuItem("BDFrameWork工具箱/资源打包/表格->生成Class" ,false , 3)]
    public static void ExecuteGenTableCalss()
    {
        Excel2Code.GenCode();
    }
    
    [MenuItem("BDFrameWork工具箱/资源打包/表格->生成SQLite" ,false , 4)]
    public static void ExecuteGenTable()
    {
        Editor_GenLocalDataPacket.GenTableCofig();
    }
    
    
    [MenuItem("BDFrameWork工具箱/资源打包/一键打包" ,false , 51)]
    public static void GenResouceall()
    {
        //Editor_VersionCtrl.GenTableCofig();
    }
}
