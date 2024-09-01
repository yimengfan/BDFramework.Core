using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 按钮枚举
    /// </summary>
    public enum BDEditorGlobalMenuItemOrderEnum
    {
        //**********************基础编辑器**************************
        BDFrameworkGuid = 0,
        BDFrameworkSetting = 1,

        //**********************BuildPipeline**************************
        BuildPipeline = 50,
        BuildPackage_DLL = 52,
        BuildPackage_Assetbundle = 53,
        BuildPackage_Table_Table2Class = 54,
        BuildPackage_Table_GenSqlite = 55,
        BuildPackage_Table_Json2Sqlite = 56,
        BuildPipeline_NetProtocol_Proto2Class = 57,
        BuildPipeline_BuildPackage =58,
        //**********************PublishPipeline**************************
        PublishPipeline = 100,
        PublishPipeline_BuildAsset = PublishPipeline + 1,
        PublishPipeline_PublishPackage = PublishPipeline + 2,

        //**********************HotfixPipeline**************************
        HotfixPipeline = 111,
        /****************DevOps***************************/
        DevOps = 121,
        //**********************TestPipeline**************************
        TestPepeline = 201,
        TestPepelineEditor = TestPepeline + 1,

    }

    /// <summary>
    /// 按钮排版
    /// </summary>
    static public class MenuItems
    {
        public static void Open()
        {
            EditorWindow_BDFrameworkStart.Open();
        }
        // [MenuItem("BDFrameWork工具箱/---Build Pipeline----", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPipeline)]
        // static void BUILD_PIPELINE()
        // {
        // }
        //
        // [MenuItem("BDFrameWork工具箱/---Publish Pipeline----", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline)]
        // static void PUBLISH_PIPELINE()
        // {
        // }
        // [MenuItem("BDFrameWork工具箱/---Test Pipeline----", false, (int) BDEditorGlobalMenuItemOrderEnum.TestPepeline)]
        // static void TEST_PIPELINE()
        // {
        // }
    }
}