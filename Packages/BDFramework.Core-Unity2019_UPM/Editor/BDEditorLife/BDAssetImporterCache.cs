using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    /// <summary>
    /// 资源导入缓存
    /// </summary>
    public class BDAssetImporterCache : AssetPostprocessor
    {
        /// <summary>
        /// 上次修改Hotfix的脚本
        /// </summary>
        public static List<string> LastChangedHotfixCs { get; set; } = new List<string>();

        public static bool IsChangedHotfixCode
        {
            get
            {
                return LastChangedHotfixCs.Count > 0;
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            LastChangedHotfixCs = new List<string>();
            //
            foreach (string str in importedAssets)
            {
                if (str.Contains("@hotfix") && str.EndsWith(".cs"))
                {
                    LastChangedHotfixCs.Add(str);

                    Debug.Log("修改hotfix代码");
                }
            }
        }
    }
}