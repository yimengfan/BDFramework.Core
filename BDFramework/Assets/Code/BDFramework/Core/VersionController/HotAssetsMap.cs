using System.Collections.Generic;
using BDFramework.Helper;
using UnityEngine;

namespace BDFramework.VersionContrller
{
    /// <summary>
    /// 这个是热更新资源hash的映射关系
    /// </summary>
    static public class HotAssetsMap
    {
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Load(AssetConfig config)
        {
            assetMap = new Dictionary<string, string>();
            foreach (var item in config.Assets)
            {
               assetMap[item.LocalPath] = item.HashName;
            }
        }

        /// <summary>
        /// 直接加载文件
        /// </summary>
        static public void Load()
        {
            var platform = Utils.GetPlatformPath(Application.platform);
            var config = platform+ "_VersionConfig.json";
             
        }


        /// <summary>
        /// 资源表
        /// </summary>
        static Dictionary<string, string> assetMap;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        static public string GetAssetLocalHash(string localPath)
        {
            string hash = null;
            assetMap.TryGetValue(localPath, out hash);
            return hash;
        }
    }
}