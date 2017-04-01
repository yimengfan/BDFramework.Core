using UnityEditor;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System;

public class ScriptBiuldDll_Windows: EditorWindow
{

    private void OnGUI()
    {

    }

    /// <summary>
    /// 编译DLL
    /// </summary>
   private void BuildDLL()
    {
        //编译项目的base.dll
        EditorUtility.DisplayProgressBar("打包dll", "编译base.dll ..", 0.1f);

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
        ScriptBiuld_Service.BuildDll(directs, baseDllPath);

        //原谅我作假
        {
            //进度
            EditorUtility.DisplayProgressBar("打包DLL", "编译Base.dll[完成]  ..", 0.4f);
            //等待500ms
            Thread.Sleep(500);
            //进度
            EditorUtility.DisplayProgressBar("打包DLL", "编译hotfix.dll  ..", 0.7f);
        }
        //编译hotfix.dll
        var hotfix = Application.dataPath + "/Script/LogicModule";
        var hotfixDllPath = Application.streamingAssetsPath + "/hot_fix.dll";

        ScriptBiuld_Service.BuildDll(new string[] { hotfix }, hotfixDllPath, new string[] { baseDllPath });
        //进度
        EditorUtility.DisplayProgressBar("打包DLL", "编译hotfix.dll  ..", 0.99f);
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("DLL打包", "DLL打包成功!\n位于StreamingAssets下", "OK");
    }
}
