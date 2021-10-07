using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using ServiceStack.Text;
using UnityEngine;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// manifest 
    /// </summary>
    public class AssetbundleConfigLoder
    {
        
        /// <summary>
        /// 是否为hash命名
        /// </summary>
        public bool IsHashName { get; set; } = true;

        
        /// <summary>
        /// 资源列表
        /// </summary>
        public List<AssetBundleItem> AssetbundleItemList { get; set; } = new List<AssetBundleItem>();

        /// <summary>
        /// 加载路径名-资源数据
        /// </summary>
        public Dictionary<string, int> LoadPathIdxMap { get; private set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        
        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="onLoaded"></param>
        public void Load(string configPath)
        {
            if (File.Exists(configPath))
            {
                var content = File.ReadAllText(configPath);
                this.AssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(content);
                foreach (var assetBundleItem in this.AssetbundleItemList)
                {
                    //可以被加载的资源
                    if (!string.IsNullOrEmpty(assetBundleItem.LoadPath))
                    {
                        LoadPathIdxMap[assetBundleItem.LoadPath] = assetBundleItem.Id;
                    }
                }
                
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + configPath);
            }
        }
        

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<string>) GetDependAssetsByName(string assetName)
        {
            var assetbundleItem = GetAssetBundleData(assetName);
            if (assetbundleItem != null)
            {
                var retlist = new List<string>(assetbundleItem.DependAssetIds.Count);
                //找到依赖资源
                foreach (var idx in assetbundleItem.DependAssetIds)
                {
                    var abItem = this.AssetbundleItemList[idx];
                    retlist.Add(abItem.AssetBundlePath);
                }

                return (assetbundleItem, retlist);
            }

            BDebug.LogError("【config】不存在资源:" + assetName);
            return (null, null);
        }


        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleData(string assetLoadPath)
        {
            if (!string.IsNullOrEmpty(assetLoadPath))
            {
                if (this.LoadPathIdxMap.TryGetValue(assetLoadPath, out var idx))
                {
                    return this.AssetbundleItemList[idx];
                }
            }

            return null;
        }
        
        

    }
}