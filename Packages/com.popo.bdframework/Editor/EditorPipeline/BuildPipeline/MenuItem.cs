using BDFramework.Editor.PublishPipeline;
using BDFramework.Editor.Table;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorPipeline.BuildPipeline
{
    static public class EditorMenuItem
    {
        [MenuItem("BDFrameWork工具箱/Odin BuildPipeline")]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_BuildPipeline>("BuildPipeline");
#if ODIN_INSPECTOR
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1000, 800);
#endif
        }
        
        [MenuItem("BDFrameWork工具箱/1.DLL打包", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_DLL)]
        public static void OpenDLL()
        {
            var window = EditorWindow.GetWindow<EditorWindow_PublishAssets>(false, "发布资源");
            window.Show();
            window.Focus();
        }
        
        [MenuItem("BDFrameWork工具箱/2.AssetBundle打包", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Assetbundle)]
        public static void OpenAB()
        {
            var window = EditorWindow.GetWindow<EditorWindow_PublishAssets>(false, "发布资源");
            window.Show();
            window.Focus();
        }
        [MenuItem("BDFrameWork工具箱/3.表格/表格预览", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Table_GenSqlite - 1)]
        public static void OpenSQL()
        {
            var win = EditorWindow.GetWindow<EditorWindow_Table>();
            win.Show();
        }

        [MenuItem("BDFrameWork工具箱/5.构建包体", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPipeline_BuildPackage)]
        public static void NULL()
        {
            var window = EditorWindow.GetWindow<EditorWindow_BuildPipeline>("BuildPipeline");
#if ODIN_INSPECTOR
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1000, 800);
#endif
        }
        /// <summary>
        /// 显示依赖
        /// </summary>
        [MenuItem("Assets/Find Asset Dependencise")]
        static void ShowAssetDependencies()
        {
            var objects = Selection.objects;
            if (objects.Length > 0)
            {
                var path = AssetDatabase.GetAssetPath(objects[0]);
                var dependencies = AssetDatabase.GetDependencies(path);
                Debug.Log($"<color=yellow>获取依赖:{path}</color>");
                foreach (var depend in dependencies)
                {
                    Debug.Log($"[依赖] {depend}");
                }
            }
        }
    }
}
