using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BDEditorMenu
{

    [MenuItem("BDFrameWork工具箱/资源打包/AssetBundle")]
    public static void ExecuteAssetBundle()
    {
        var  window = (ScriptBiuldDll_Windows)EditorWindow.GetWindow(typeof(ScriptBiuldDll_Windows));   
        window.Show();
    }
}
