using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework;
using LitJson;
using UnityEditor;
using UnityEngine;

public class ExpoterMainProjectAsset
{
    public class PackageData
    {
        public string version = "null";
    }

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

        var exporterDirectoryList = new string[]
        {
            "Assets/3rdPlugins/Dotween",                          // 第三方插件
            "Assets/Code/BDFramework.Game",                       //Game
            "Assets/Scenes",                                      //Scene
            "Assets/AssetGraph/BResourceAssetBundleConfig.asset", //SG
            "Assets/link.xml",

        };
        var exportAssets = new List<string>();
        foreach (var direct in exporterDirectoryList)
        {
            if (Directory.Exists(direct))
            {
                var fs = Directory.GetFiles(direct, "*.*", SearchOption.AllDirectories);
                exportAssets.AddRange(fs);
            }
            else if(File.Exists(direct))
            {
                exportAssets.Add(direct);
            }
        }

        var exportfs = exportAssets.Where((ex) => !ex.EndsWith(".meta")).ToArray();
        //版本信息修改

        #region version管理

        //package 版本
        var packageDataPath = AssetDatabase.GUIDToAssetPath("e56f3b41caab3304194319691ec2ebbb");
        var packageContent  = File.ReadAllText(packageDataPath);
        var pckage          = JsonMapper.ToObject<PackageData>(packageContent);

        //Editor Runtime版本
        var editorRuntimeVersionPath = AssetDatabase.GUIDToAssetPath("996622d6f14afc44dbd42c1cdfa8a362");
        var config                   = new BDFrameWorkConfig();
        config.Version = pckage.version;
        File.WriteAllText(editorRuntimeVersionPath, JsonMapper.ToJson(config));
        //Asset目录版本
        var assetPathPath = AssetDatabase.GUIDToAssetPath("924d970067c935c4f8b818e6b4ab9e07");
        File.WriteAllText(assetPathPath, pckage.version);
        AssetDatabase.Refresh();

        #endregion


        //最后,导出Asset.Package
        ExportPackageOptions op          = ExportPackageOptions.Default;
        var                  packagePath = AssetDatabase.GUIDToAssetPath("69227cf6ea5304641ae95ffb93874014");
        //AssetDatabase.ImportPackage(packagePath,true);
        AssetDatabase.ExportPackage(exportfs, packagePath, op);
        //重新生成clr分析文件
        EditorWindow_ScriptBuildDll.GenCLRBindingByAnalysis();
        //debug
        Debug.Log("导出成功:" + packagePath);
    }
}