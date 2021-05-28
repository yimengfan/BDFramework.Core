using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    /// <summary>
    /// 资源导入缓存
    /// </summary>
    public class BDAssetImporterCache : AssetPostprocessor
    {
        private static string importerCahcePath = BDApplication.BDEditorCachePath + "/ImporterCache";
        /// <summary>
        /// 上次修改Hotfix的脚本
        /// </summary>
        public static List<string> LastChangedHotfixCs { get; set; } = new List<string>();

        /// <summary>
        /// 是否修改了热更代码
        /// </summary>
        public static bool IsChangedHotfixCode
        {
            get
            {
                if (File.Exists(importerCahcePath))
                {
                    LastChangedHotfixCs =  JsonMapper.ToObject<List<string>>(File.ReadAllText(importerCahcePath));
                }
                
                return LastChangedHotfixCs.Count > 0;
            }
        }

        /// <summary>
        /// 资源导入监听
        /// </summary>
        /// <param name="importedAssets"></param>
        /// <param name="deletedAssets"></param>
        /// <param name="movedAssets"></param>
        /// <param name="movedFromAssetPaths"></param>
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
                    
                }
            }
            foreach (string str in movedAssets)
            {
                if (str.Contains("@hotfix") && str.EndsWith(".cs"))
                {
                    LastChangedHotfixCs.Add(str);
                    
                }
            }
            //写入本地
            FileHelper.WriteAllText(importerCahcePath,JsonMapper.ToJson(LastChangedHotfixCs));
            
        }
    }
}