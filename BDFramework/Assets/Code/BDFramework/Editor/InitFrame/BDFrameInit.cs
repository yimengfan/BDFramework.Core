using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Path = System.IO.Path;

namespace BDFramework.Editor
{
    static public class BDFrameInit
    {

        static private List<string> PathList = new List<string>()
        {
            "3rdPlugins",
            "Code",
            "Code/Game",
            "Code/Game/Data",
            "Code/Game/ScreenView",
            "Code/Game/Windows",
            "Code/Game/Window_MVC",
            "Resource/AssetBundle",
            "Resource/Effect",
            "Resource/Img",
            "Resource/Model",
            "Resource/Resources",
            "Resource/Table",
            "Scene",
            "StreammingAssets",
        };
   
        static public void Init()
        {
            foreach (var p in PathList)
            {
                var _p = Path.Combine(Application.dataPath, p);
                if (Directory.Exists(_p) == false)
                {
                    Directory.CreateDirectory(_p);
                }
            }

            EditorUtility.DisplayDialog("提示", "目录生成完毕,请放心食用~", "OK");
            AssetDatabase.Refresh();
        }
    }

}

