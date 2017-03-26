using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class CreateAssetBundleForXmlVersion
{
    public static void Execute(UnityEditor.BuildTarget target)
    {
        string SavePath = AssetBundleController.GetPlatformPath(target);
        Object obj = AssetDatabase.LoadAssetAtPath(SavePath + "VersionNum/VersionNum.bytes", typeof(Object));
        BuildPipeline.BuildAssetBundle(obj, null, SavePath + "VersionNum/VersionNum.assetbundle", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, target);

        AssetDatabase.Refresh();
    }

    static string ConvertToAssetBundleName(string ResName)
    {
        return ResName.Replace('/', '.');
    }

}
