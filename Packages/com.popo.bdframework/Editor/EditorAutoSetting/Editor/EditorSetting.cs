using UnityEditor;
using UnityEngine;


/// <summary>
/// 框架导入的自动设置
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
    }
}
