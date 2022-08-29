using System;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using LitJson;
using UnityEditor;

namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// BuildingAssets的缓存信息
    /// 只在Editor编辑器模式下生效，用以减少每次预览的开销
    /// Building模式会重新搜集
    /// </summary>
    public class EditorAssetInfosCache : AssetPostprocessor
    {
        static public void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool isAssetChanged = false;
            if (!isAssetChanged)
            {
                //非脚本修改，剩下的则是资源修改
                var ret = importedAssets.FirstOrDefault((a) => a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
                var ret2 = deletedAssets.FirstOrDefault((a) => a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
                var ret3 = movedAssets.FirstOrDefault((a) => a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
                var ret4 = movedFromAssetPaths.FirstOrDefault((a) => a.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
                if (ret != null || ret2 != null || ret3 != null || ret4 != null)
                {
                    isAssetChanged = true;
                }
            }

            //资源变动，触发资源cache修改
            if (isAssetChanged)
            {
                var buildingAssetinfo = GetBuildingAssetInfosCache();
                {
                    foreach (var asset in importedAssets)
                    {
                        OnCacheAssetChanged(asset);
                    }

                    foreach (var asset in deletedAssets)
                    {
                        OnCacheAssetChanged(asset);
                    }

                    foreach (var asset in movedFromAssetPaths)
                    {
                        OnCacheAssetChanged(asset);
                    }
                }
                SaveBuildingAssetInfosCache(buildingAssetinfo);
            }
        }

        /// <summary>
        /// info
        /// </summary>
        static private BuildAssetInfos buildAssetInfos;

        
        //路径
        public static string PATH = BApplication.BDEditorCachePath + "/BuildPipelineV2_EditorAssetInfosCache";
        /// <summary>
        /// 获取cache
        /// </summary>
        /// <returns></returns>
        static public BuildAssetInfos GetBuildingAssetInfosCache()
        {
            //每次构建新对象返回
            if (File.Exists(PATH))
            {
                var content = File.ReadAllText(PATH);
                buildAssetInfos = JsonMapper.ToObject<BuildAssetInfos>(content);
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
        static public void OnCacheAssetChanged(string path)
        {
            path = IPath.ReplaceBackSlash(path);
            buildAssetInfos.AssetInfoMap.Remove(path.ToLower());
        }
    }
}
