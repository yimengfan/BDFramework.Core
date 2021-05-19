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
        var targetPath = "Assets/Code/BDFramework.Game/ILRuntime/Binding/Analysis";
        //1.分析之前先删除,然后生成临时文件防止报错
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }
        var fileContent = @"
        namespace ILRuntime.Runtime.Generated
        {
            class CLRBindings
            {
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector2> s_UnityEngine_Vector2_Binding_Binder = null;
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector3> s_UnityEngine_Vector3_Binding_Binder = null;
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector4> s_UnityEngine_Vector4_Binding_Binder = null;
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Quaternion> s_UnityEngine_Quaternion_Binding_Binder = null;
                public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
                {
                }
            } 
        }   ";
        FileHelper.WriteAllText(targetPath + "/CLRBindings.cs", fileContent);

        AssetDatabase.Refresh();
            
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