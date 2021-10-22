using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using ServiceStack.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// loadpath包装
    /// 加载路径名-资源数据
    /// </summary>
    public class LoadPathMap : Dictionary<string, int>
    {
        public LoadPathMap() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }

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
        public List<AssetBundleItem> AssetbundleItemList { get; private set; } = new List<AssetBundleItem>();

        /// <summary>
        /// 资源类型-资源映射
        /// 这里拆了2个map映射，提高加载检索速度，也能加载同名不同类型文件
        /// </summary>
        private Dictionary<string, LoadPathMap> assetTypeIdxMap { get; set; } = new Dictionary<string, LoadPathMap>();

        /// <summary>
        /// 资源类型列表
        /// </summary>
        public List<string> AssetTypeList { get; private set; } = new List<string>();

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="onLoaded"></param>
        public void Load(string configPath, string assetTypePath)
        {
            //资源类型配置
            if (File.Exists(assetTypePath))
            {
                var content = File.ReadAllText(assetTypePath);
                this.AssetTypeList = CsvSerializer.DeserializeFromString<List<string>>(content);

                //创建不同类型的映射表
                foreach (var assetType in this.AssetTypeList)
                {
                    this.assetTypeIdxMap[assetType] = new LoadPathMap();
                }
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + configPath);
            }

            //资源配置
            if (File.Exists(configPath))
            {
                var content = File.ReadAllText(configPath);
                this.AssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(content);
                foreach (var abItem in this.AssetbundleItemList)
                {
                    //可以被加载的资源
                    if (!string.IsNullOrEmpty(abItem.LoadPath))
                    {
                        var assettype = this.AssetTypeList[abItem.AssetType];
                        var map       = this.assetTypeIdxMap[assettype];
                        map[abItem.LoadPath] = abItem.Id;
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
        public (AssetBundleItem, List<string>) GetDependAssetsByName<T>(string assetName) where T : Object
        {
            var assetbundleItem = GetAssetBundleData<T>(assetName);
            if (assetbundleItem != null)
            {
                var retlist = new List<string>(assetbundleItem.DependAssetIds.Count);
                //找到依赖资源
                for (int i = 0; i < assetbundleItem.DependAssetIds.Count; i++)
                {
                    var idx    = assetbundleItem.DependAssetIds[i];
                    var abItem = this.AssetbundleItemList[idx];
                    retlist.Add(abItem.AssetBundlePath);
                }

                return (assetbundleItem, retlist);
            }

            BDebug.LogError("【config】不存在资源:" + assetName);
            return (null, null);
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<string>) GetDependAssetsByName(string assetName)
        {
            AssetBundleItem retABItem = null;
            for (int i = 0; i < this.AssetbundleItemList.Count; i++)
            {
                var abitem = this.AssetbundleItemList[i];
                if (abitem.LoadPath.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                {
                    retABItem = abitem;
                    break;
                }
            }

            if (retABItem != null)
            {
                var retlist = new List<string>(retABItem.DependAssetIds.Count);
                //找到依赖资源
                for (int i = 0; i < retABItem.DependAssetIds.Count; i++)
                {
                    var idx    = retABItem.DependAssetIds[i];
                    var abItem = this.AssetbundleItemList[idx];
                    retlist.Add(abItem.AssetBundlePath);
                }

                return (retABItem, retlist);
            }


            BDebug.LogError("【config】不存在资源:" + assetName);
            return (null, null);
        }


        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleData<T>(string assetLoadPath) where T : Object
        {
            if (!string.IsNullOrEmpty(assetLoadPath))
            {
                var type = typeof(T).FullName;

                if (this.assetTypeIdxMap.TryGetValue(type, out var assetMap))
                {
                    if (assetMap.TryGetValue(assetLoadPath, out var idx))
                    {
                        return this.AssetbundleItemList[idx];
                    }
                }
            }

            return null;
        }
    }
}