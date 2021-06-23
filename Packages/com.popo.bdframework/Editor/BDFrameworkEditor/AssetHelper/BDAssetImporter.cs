using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 资源导入监听管理
    /// 这里只能保存到本地，每次修改cs unity都会清空内存变量，重新build dll
    /// </summary>
    public class BDAssetImporter : AssetPostprocessor
    {
        
        /// <summary>
        /// 资源导入缓存
        /// 导入表示则有修改
        /// </summary>
        public class BDAssetImpoterCache
        {
           public  List<string> HotfixList { get; set; } = new List<string>();
        }
        
        /// <summary>
        /// 缓存路径
        /// </summary>
        private static string ImporterCahcePath
        {
            get
            {
                return BDApplication.BDEditorCachePath + "/ImporterCache";
            }
        }
        
        /// <summary>
        /// 上次修改Hotfix的脚本
        /// </summary>
        public static BDAssetImpoterCache CacheData;

        /// <summary>
        /// 是否修改过hotfix文件
        /// </summary>
        public static bool IsChangedHotfixCode
        {
            get
            {
                if (File.Exists(ImporterCahcePath))
                {
                    CacheData = JsonMapper.ToObject<BDAssetImpoterCache>(File.ReadAllText(ImporterCahcePath));
                }

                return CacheData.HotfixList.Count > 0;
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
            CacheData = new BDAssetImpoterCache();
            //1.搜集热更脚本变更
            foreach (string str in importedAssets)
            {
                if (str.Contains("@hotfix") && str.EndsWith(".cs"))
                {
                    CacheData.HotfixList.Add(str);
                }
            }
            foreach (string str in movedAssets)
            {
                if (str.Contains("@hotfix") && str.EndsWith(".cs"))
                {
                    CacheData.HotfixList.Add(str);
                }
            }
            FileHelper.WriteAllText(ImporterCahcePath, JsonMapper.ToJson(CacheData));
            
            //2.判断是否导入Odin
            foreach (string str in importedAssets)
            {
                if (str.Contains("Sirenix.OdinInspector.Attributes.dll"))
                {
                    var path = AssetDatabase.GUIDToAssetPath("b072c123447549fa81bb03f3ddebec80");
                    if (File.Exists(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                    break;
                }
            }
            //编译dll
            BDFrameEditorLife.BuildHotfixDll();
        }
    }
}