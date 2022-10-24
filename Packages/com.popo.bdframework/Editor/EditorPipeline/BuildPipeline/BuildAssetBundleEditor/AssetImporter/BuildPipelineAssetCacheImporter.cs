using System;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.BuildPipeline.AssetBundle
{
    /// <summary>
    /// BuildingAssets的缓存信息
    /// 只在Editor编辑器模式下生效，用以减少每次预览的开销
    /// Building模式会重新搜集
    /// </summary>
    public class BuildPipelineAssetCacheImporter : AssetPostprocessor
    {
        /// <summary>
        /// 缓存
        /// </summary>
        static private BuildAssetInfos AssetInfoCache;

        static public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool isAssetChanged = false;
            if (!isAssetChanged)
            {
                //非脚本修改，剩下的则是资源修改
                importedAssets = importedAssets.Where((a) => !a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
                deletedAssets = deletedAssets.Where((a) => !a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
                movedAssets = movedAssets.Where((a) => !a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
                movedFromAssetPaths = movedFromAssetPaths.Where((a) => !a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (importedAssets?.Count() > 0 || deletedAssets?.Count() > 0 || movedAssets?.Count() > 0 || movedFromAssetPaths?.Count() > 0)
                {
                    isAssetChanged = true;
                }
            }

            //资源变动，触发资源cache修改
            if (isAssetChanged)
            {
                if (AssetInfoCache == null) AssetInfoCache = GetBuildingAssetInfosCache();
                {
                    foreach (var asset in importedAssets)
                    {
                        if (!string.IsNullOrEmpty(asset))
                        {
                            OnAssetChanged(asset.ToLower());
                        }
                    }

                    foreach (var asset in deletedAssets)
                    {
                        if (!string.IsNullOrEmpty(asset))
                        {
                            OnAssetDelete(asset.ToLower());
                        }
                    }

                    foreach (var asset in movedFromAssetPaths)
                    {
                        if (!string.IsNullOrEmpty(asset))
                        {
                            OnAssetChanged(asset.ToLower());
                        }
                    }
                }
                SaveBuildingAssetInfosCache(AssetInfoCache);
            }
        }


        //路径
        public static string PATH = BApplication.BDEditorCachePath + "/BuildPipelineV2_EditorAssetInfosCache";

        /// <summary>
        /// 获取cache
        /// </summary>
        /// <returns></returns>
        static public BuildAssetInfos GetBuildingAssetInfosCache()
        {
            BuildAssetInfos buildAssetInfos;
            //每次构建新对象返回
            if (File.Exists(PATH))
            {
                var content = File.ReadAllText(PATH);
                buildAssetInfos = JsonMapper.ToObject<BuildAssetInfos>(content);
                //检测资产是否存在
                var keys = buildAssetInfos.AssetInfoMap.Keys.ToList();

                foreach (var key in keys)
                {
                    if (!File.Exists(key))
                    {
                        buildAssetInfos.AssetInfoMap.Remove(key);
                        Debug.Log("remove:" + key);
                    }
                }
            }
            else
            {
                buildAssetInfos = new BuildAssetInfos();
            }


            return buildAssetInfos;
        }

        /// <summary>
        /// 保存缓存信息
        /// </summary>
        /// <returns></returns>
        static public void SaveBuildingAssetInfosCache(BuildAssetInfos buildAssetInfos)
        {
            var content = JsonMapper.ToJson(buildAssetInfos, true);
            FileHelper.WriteAllText(PATH, content);
        }

        /// <summary>
        /// 当缓存资源修改
        /// </summary>
        /// <param name="path"></param>
        static public void OnAssetChanged(string path)
        {
            path = IPath.ReplaceBackSlash(path);
            var assetInfo = AssetInfoCache.GetAssetInfo(path, false);
            //增加
            if (assetInfo != null)
            {
                AssetInfoCache.AssetInfoMap.Remove(path);
            }

            if (File.Exists(path))
            {
                AssetInfoCache.AddAsset(path);
            }
        }

        /// <summary>
        /// 当缓存删除`
        /// </summary>
        /// <param name="path"></param>
        static public void OnAssetDelete(string path)
        {
            path = IPath.ReplaceBackSlash(path);
            AssetInfoCache.AssetInfoMap.Remove(path.ToLower());
        }
    }
}
