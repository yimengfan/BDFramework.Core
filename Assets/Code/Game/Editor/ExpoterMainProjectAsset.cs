using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ExpoterMainProjectAsset
{
    [MenuItem("BDFrame开发辅助/导出主工程资源")]
    static void Exprot()
    {
        var exporterDirectoryList = new string[] {"Assets/Code/BDFramework.Game", "Assets/Scenes",};
        var exportAssets          = new List<string>();
        foreach (var direct in exporterDirectoryList)
        {
            var fs = Directory.GetFiles(direct, "*.*", SearchOption.AllDirectories);
            exportAssets.AddRange(fs);
        }

        var exportfs = exportAssets.Where((ex) => !ex.EndsWith(".meta")).ToArray();
        //导出
        ExportPackageOptions op          = ExportPackageOptions.Default;
        var                  packagePath = AssetDatabase.GUIDToAssetPath("69227cf6ea5304641ae95ffb93874014");
        //AssetDatabase.ImportPackage(packagePath,true);
        AssetDatabase.ExportPackage(exportfs, packagePath, op);
        Debug.Log("导出成功:" + packagePath);
    }
}