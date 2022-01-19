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
        if (!PlayerSettings.allowUnsafeCode)
        {
            PlayerSettings.allowUnsafeCode = true;
            Debug.Log("【AutoSetting】allowUnsafeCode = true.");
        }

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
    }
}