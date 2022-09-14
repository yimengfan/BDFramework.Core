using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorPipeline.BuildPipeline
{
    static public class EditorMenuItem
    {
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
