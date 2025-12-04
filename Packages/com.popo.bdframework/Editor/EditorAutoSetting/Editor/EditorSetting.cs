using BDFramework.Editor.Unity3dEx;
using Unity.InternalAPIEngineBridge;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 框架导入的编辑器自动设置
/// </summary>
[InitializeOnLoad]
static public class EditorSetting
{
    static EditorSetting()
    {
        #region 设置处理
        //设置允许unsafe
        if (!PlayerSettings.allowUnsafeCode)
        {
            PlayerSettings.allowUnsafeCode = true;
            Debug.Log("【AutoSetting】allowUnsafeCode = true.");
        }

        //关闭dll校验
        if (PlayerSettings.assemblyVersionValidation)
        {
            PlayerSettings.assemblyVersionValidation = false;

            Debug.Log("【AutoSetting】assemblyVersionValidation = false.");
        }

        //设置生成所有的csproj
        var settingName = "unity_generate_all_csproj";
        var value = EditorPrefs.GetBool(settingName);
        if (!value)
        {
            EditorPrefs.SetBool(settingName, true);
            Debug.Log($"【AutoSetting】{settingName}= true.");
        }
        

        #endregion

        #region 宏处理
        BuildTargetGroup[] _supportBuildTargetGroup = new BuildTargetGroup[]
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            /***********新增pc平台************/
            BuildTargetGroup.Standalone,
            
        };
        
        foreach (var bt in _supportBuildTargetGroup)
        {
            Unity3dEditorEx.AddSymbols(bt,"ENABLE_IL2CPP");
            Unity3dEditorEx.AddSymbols(bt,"ENABLE_HYCLR");
        }

        #endregion
      
        PackageManagerEx.SetBDFramworkOpenUpmEnv();
        
      //  DeEditorPackageManager
      
    }
}
