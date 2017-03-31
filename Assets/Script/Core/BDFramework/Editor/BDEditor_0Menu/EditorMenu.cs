using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BDEditorMenu
{

    [MenuItem("BDFrameWork工具箱/资源打包/DLL打包")]
    public static void ExecuteAssetBundle()
    {
        //var  window = (ScriptBiuldDll_Windows)EditorWindow.GetWindow(typeof(ScriptBiuldDll_Windows),false,"dll打包工具");   
        //window.Show();

        //编译项目的base.dll
        var directs = Directory.GetDirectories(Application.dataPath + "/Script");
        var baseDllPath = Application.streamingAssetsPath + "/Base.dll";
        for (int i = directs.Length - 1; i >= 0; i--)
        {
            if (directs[i].IndexOf("LogicModule") != -1)
            {
                directs[i] = "";
                break;
            }
        }
        //
        ScriptBiuld_Service.BuildDll(directs, baseDllPath);
        //编译hotfix.dll
        var hotfix = Application.dataPath + "/Script/LogicModule";
        var hotfixDllPath = Application.streamingAssetsPath + "/hot_fix.dll";

        ScriptBiuld_Service.BuildDll(new string[] { hotfix }, hotfixDllPath, new string[] { baseDllPath });
    }
}
