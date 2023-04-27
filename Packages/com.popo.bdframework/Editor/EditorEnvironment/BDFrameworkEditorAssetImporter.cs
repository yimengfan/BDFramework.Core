using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixScript;
using BDFramework.Editor.Unity3dEx;
using BDFramework.Editor.WorkFlow;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Environment
{
    /// <summary>
    /// 资源导入监听管理
    /// 这里只能保存到本地，每次修改cs unity都会清空内存变量，重新build dll
    /// </summary>
    public class BDFrameworkAssetImporter : AssetPostprocessor
    {
        /// <summary>
        /// 资源导入缓存
        /// 导入表示则有修改
        /// </summary>
        public class BDAssetImpoterCache
        {
            public List<string> HotfixList { get; set; } = new List<string>();
        }

        /// <summary>
        /// 缓存路径
        /// </summary>
        private static string ImporterCahcePath
        {
            get { return BApplication.BDEditorCachePath + "/ImporterCache"; }
        }

        static private BDAssetImpoterCache _CacheData;

        /// <summary>
        /// 上次修改Hotfix的脚本
        /// 这里为了解决 在play模式下修改代码，play结束后 editordll 生命被释放，所以需要缓存
        /// </summary>
        public static BDAssetImpoterCache CacheData
        {
            get
            {
                if (File.Exists(ImporterCahcePath) && _CacheData == null)
                {
                    _CacheData = JsonMapper.ToObject<BDAssetImpoterCache>(File.ReadAllText(ImporterCahcePath));
                }

                return _CacheData;
            }
        }


        /// <summary>
        /// 资源导入监听
        /// </summary>
        /// <param name="importedAssets"></param>
        /// <param name="deletedAssets"></param>
        /// <param name="movedAssets"></param>
        /// <param name="movedFromAssetPaths"></param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            _CacheData = new BDAssetImpoterCache();
            //1.搜集热更脚本变更
            foreach (string assetPath in importedAssets)
            {
                if (ScriptBuildTools.IsHotfixScript(assetPath))
                {
                    CacheData.HotfixList.Add(assetPath);
                }
            }

            foreach (string assetPath in movedAssets)
            {
                if (ScriptBuildTools.IsHotfixScript(assetPath))
                {
                    CacheData.HotfixList.Add(assetPath);
                }
            }

            FileHelper.WriteAllText(ImporterCahcePath, JsonMapper.ToJson(CacheData));
            //编译dll
            if (CacheData.HotfixList.Count > 0)
            {
                HotfixCodeWorkFlow.OnCodeChanged();
            }

            //2.判断是否导入Odin，是则加入命名空间
            foreach (string impoter in importedAssets)
            {
                if (impoter.Contains("Sirenix.OdinInspector.Attributes.dll"))
                {
                    Unity3dEditorEx.AddSymbols("ODIN_INSPECTOR");

                    break;
                }
            }
        }
    }
}