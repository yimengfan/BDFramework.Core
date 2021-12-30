using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 按钮枚举
    /// </summary>
    public enum BDEditorGlobalMenuItemOrderEnum
    {
        BDFrameStart                         =0,
        BDSetting                            = 1,
        BuildPackage_DLL                     =52,
        BuildPackage_Assetbundle             =53,
        BuildPackage_Table_Table2Class       =54,
        BuildPackage_Table_GenSqlite         =55,
        BuildPackage_Table_Json2Sqlite       =56,
        BuildPackage_NetProtocol_Proto2Class =57,
        //
        BuildPipeline=100,
        BuildPipeline_BuildAsset =101,
        BuildPipeline_PublishPackage =102,
        BuildPipeline_CICD=103,

        
        //测试用例
        //Testrunner
        TestRunnerEditor=151,
        
    }
    
    /// <summary>
    /// 按钮排版
    /// </summary>
    static public class MenuItems
    {
        [MenuItem("BDFrameWork工具箱/框架引导", false, (int) BDEditorGlobalMenuItemOrderEnum.BDFrameStart)]
        public static void Open()
        {
            EditorWindow_BDFrameworkStart.Open();
        }

    }
}