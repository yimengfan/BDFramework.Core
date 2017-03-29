using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BDEditorMenu
{

    [MenuItem("BDFrameWork工具箱/资源打包/DLL打包")]
    public static void ExecuteAssetBundle()
    {
        //var  window = (ScriptBiuldDll_Windows)EditorWindow.GetWindow(typeof(ScriptBiuldDll_Windows),false,"dll打包工具");   
        //window.Show();
        var inpath = Application.dataPath + "/Script/Core";
        var inpath2 = Application.dataPath + "/Script/MainSceneScript";
     //  var inpath = Application.dataPath + "/Script/LogicModule";
        //var inpath2 = Application.dataPath + "/Script/Core/BDFramework/BD_Screenview";
        ScriptBiuld_Service.BuildDll(new string[] { inpath,inpath2 }, "D:/Core.dll");
    }
}
