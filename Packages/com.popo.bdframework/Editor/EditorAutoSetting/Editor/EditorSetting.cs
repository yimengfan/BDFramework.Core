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

        //
        BuildTargetGroup[] _supportBuildTargetGroup = new BuildTargetGroup[]
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            /***********新增pc平台************/
            BuildTargetGroup.Standalone,
        };

        foreach (var bt in _supportBuildTargetGroup)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
            if (!symbols.Contains("ENABLE_IL2CPP"))
            {
                string str = "";
                if (!string.IsNullOrEmpty(symbols))
                {
                    if (!str.EndsWith(";"))
                    {
                        str = symbols + ";ENABLE_IL2CPP";
                    }
                    else
                    {
                        str = symbols + "ENABLE_IL2CPP";
                    }
                }
                else
                {
                    str = "ENABLE_IL2CPP";
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, str);
            }
        }
    }
}
