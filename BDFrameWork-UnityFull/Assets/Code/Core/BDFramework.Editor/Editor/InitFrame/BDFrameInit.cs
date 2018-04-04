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
            "Code",
            "Code/Game",
            "Code/Game/Data",
            "Code/Game/ScreenView",
            "Code/Game/Windows",
            "Resource/AssetBundle",
            "Resource/Effect",
            "Resource/Img",
            "Resource/Model",
            "Resource/Resources",
            "Resource/Table",
            "Scene",
            "StreamingAssets",
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
            
            AssetDatabase.Refresh();
        }
    }

}

