using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using BDFramework.Helper;
using BDFramework.Editor.UI;

namespace BDFramework.Editor
{
    public class BDEditorMenu
    {



        [MenuItem("BDFrameWork工具箱/初始化", false, 1)]
        public static void BDInit()
        {
           BDFrameInit.Init();
        }
        
        [MenuItem("BDFrameWork工具箱/UI工作流/创建窗口", false, 51)]
        public static void OpenEditorWindow_CreateUI()
        {
            var window = (EditorWindows_UIWorker) EditorWindow.GetWindow(typeof(EditorWindows_UIWorker), false, "UI创建工具");
            window.Show();
        }
        //

        [MenuItem("BDFrameWork工具箱/1.DLL打包", false, 52)]
        public static void ExecuteBuildDLL()
        {
            var window =
                (EditorWindow_ScriptBuildDll) EditorWindow.GetWindow(typeof(EditorWindow_ScriptBuildDll), false, "DLL打包工具");
            window.Show();
        }

        [MenuItem("BDFrameWork工具箱/2.AssetBundle打包", false, 53)]
        public static void ExecuteAssetBundle()
        {
            var window =
                (EditorWindow_GenAssetBundle) EditorWindow.GetWindow(typeof(EditorWindow_GenAssetBundle), false,
                    "AB打包工具");
            window.Show();
        }

        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成Class", false, 54)]
        public static void ExecuteGenTableCalss()
        {
            Excel2Code.GenCode();
        }

        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成SQLite", false, 55)]
        public static void ExecuteGenTable()
        {
            Excel2SQLiteTools.GenSQLite(Path.Combine(Application.streamingAssetsPath,Utils.GetPlatformPath(Application.platform)));
            Debug.Log("表格导出完毕");
        }
        
        [MenuItem("BDFrameWork工具箱/3.表格/json->生成SQLite", false, 58)]
        public static void ExecuteJsonToSqlite()
        {
            Excel2SQLiteTools.GenJsonToSQLite(Path.Combine(Application.streamingAssetsPath,Utils.GetPlatformPath(Application.platform)));
            Debug.Log("表格导出完毕");
        }

       
        [MenuItem("BDFrameWork工具箱/资源压缩/图片压缩",false,56)]
        public static void ChangeTexture()
        {
            var window =(Editor_2ChangeTextureImporter)EditorWindow.GetWindow(typeof(Editor_2ChangeTextureImporter), false, "图片格式设置");
            window.Show();
        }
        [MenuItem("BDFrameWork工具箱/资源一键打包", false, 101)]
        public static void GenResouceall()
        {
            var window =(EditorWindow_OnkeyBuildAsset)EditorWindow.GetWindow(typeof(EditorWindow_OnkeyBuildAsset), false, "一键打包");
            window.Show();
        }
      
    }
}