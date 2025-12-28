using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using ServiceStack.Text;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// loadpath包装
    /// 加载路径名-资源数据
    /// </summary>
    public class LoadPathIdxMap : Dictionary<string, int>
    {
        public LoadPathIdxMap() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }

    /// <summary>
    /// manifest 
    /// </summary>
    public class AssetBundleConfigLoader
    {
        /// <summary>
        /// 资源列表
        /// </summary>
        public List<AssetBundleItem> AssetbundleItemList { get; private set; } = new List<AssetBundleItem>();

        /// <summary>
        /// 资源类型-资源映射
        /// 这里拆了2个map映射，提高加载检索速度，也能加载同名不同类型文件
        /// </summary>
        private Dictionary<string, LoadPathIdxMap> assetTypeABIdxMap { get; set; } = new Dictionary<string, LoadPathIdxMap>();

        /// <summary>
        /// 资源类型列表
        /// </summary>
        public AssetTypeConfig AssetTypes { get; private set; }

        /// <summary>
        /// 配置路径
        /// </summary>
        private string configPath { get; set; }

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="onLoaded"></param>
        public void Load(string rootDir)
        {
#if UNITY_EDITOR
            //这里用于editor加载后的覆盖写入
            this.configPath = IPath.Combine(rootDir, BResources.ART_ASSET_INFO_PATH);
#endif
            var atContent = ClientAssetsUtils.ReadAllText(rootDir, rootDir, BResources.ART_ASSET_TYPES_PATH);
            var aiContent =  ClientAssetsUtils.ReadAllText(rootDir, rootDir, BResources.ART_ASSET_INFO_PATH);
            //资源类型配置
            if (atContent ==  null || aiContent==null)
            {
                BDebug.LogError("assets配置文件不存在!!!");
                return;
            }
            var records = CsvSerializer.DeserializeFromString<List<AssetTypeConfig>>(atContent);
            this.AssetTypes = records[0];
            //创建不同类型的映射表
            foreach (var assetType in this.AssetTypes.AssetTypeList)
            {
                this.assetTypeABIdxMap[assetType] = new LoadPathIdxMap();
            }

            BDebug.LogWatchBegin("AssetbundleV2-加载config:");
            this.AssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(aiContent);
            BDebug.LogWatchEnd("AssetbundleV2-加载config:");
            //
            foreach (var abItem in this.AssetbundleItemList)
            {
                //可以被加载的资源
                if (abItem.IsLoadConfig())
                {
                    var assettype = this.AssetTypes.AssetTypeList[abItem.AssetType];
                    var map = this.assetTypeABIdxMap[assettype];
                    if (!string.IsNullOrEmpty(abItem.LoadPath))
                    {
                        map[abItem.LoadPath] = abItem.Id;
                    }
                    else if (!string.IsNullOrEmpty(abItem.GUID))
                    {
                        map[abItem.GUID] = abItem.Id;
                    }
                }
            }


            BDebug.Log(BResources.LogTag, "资源加载初始化完成,资源总量:" + this.AssetbundleItemList?.Count);
        }


        /// <summary>
        /// 获取assetbunldeItem通过guid
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleItemByGUID(string guid, Type type)
        {
            AssetBundleItem assetBundleItem = null;
            if (!string.IsNullOrEmpty(guid))
            {
                if (type == null || type == typeof(Object))
                {
                    //全局搜索,效率略低
                    assetBundleItem = this.AssetbundleItemList.FirstOrDefault((abi) => abi.GUID == guid);
                }
                else
                {
                    //指定T Map搜索
                    if (this.assetTypeABIdxMap.TryGetValue(type.FullName, out var assetMap))
                    {
                        if (assetMap.TryGetValue(guid, out var idx))
                        {
                            assetBundleItem = this.AssetbundleItemList[idx];
                        }
                    }
                }
            }

            return assetBundleItem;
        }


        /// <summary>
        /// 获取AssetBundle
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleItem(int idx)
        {
            return this.AssetbundleItemList[idx];
        }

        /// <summary>
        /// 获取单个Item
        /// Type版本
        /// </summary>
        /// <param name="loadPath">加载路径</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleItem(string loadPath, Type type = null, bool isGuid = false)
        {
            AssetBundleItem assetBundleItem = null;
            if (!string.IsNullOrEmpty(loadPath))
            {
                // BDebug.LogWatchBegin($"寻找ABItem-{loadPath}");
                if (type == null || type == typeof(Object))
                {
                    //全局搜索,效率略低
                    for (int i = 0; i < this.AssetbundleItemList.Count; i++)
                    {
                        var item = this.AssetbundleItemList[i];

                        if (!item.IsAssetBundleSourceFile())
                        {
                            if (isGuid && item.GUID == loadPath)
                            {
                                assetBundleItem = item;
                                break;
                            }
                            else if (item.LoadPath != null && item.LoadPath.Equals(loadPath, StringComparison.OrdinalIgnoreCase))
                            {
                                assetBundleItem = item;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //指定T Map搜索
                    if (this.assetTypeABIdxMap.TryGetValue(type.FullName, out var assetMap))
                    {
                        if (assetMap.TryGetValue(loadPath, out var idx))
                        {
                            assetBundleItem = this.AssetbundleItemList[idx];
                        }
                    }
                }
            }

            return assetBundleItem;
        }


        /// <summary>
        /// 获取依赖列表
        /// 主资源为最后一个
        /// </summary>
        /// <returns></returns>
        public (AssetBundleItem, IEnumerable<AssetBundleItem>) GetDependAssets<T>(string assetLoadPath) where T : Object
        {
            return GetDependAssets(assetLoadPath, typeof(T));
        }

        /// <summary>
        /// 获取依赖列表
        /// 主资源为最后一个
        /// </summary>
        /// <returns></returns>
        public (AssetBundleItem, IEnumerable<AssetBundleItem>) GetDependAssets(string assetLoadPath, Type type = null)
        {
            //获取主资源信息
            var mainABItem = GetAssetBundleItem(assetLoadPath, type);
            //获取依赖列表
            var dependList = GetDependAssets(mainABItem);
            return (mainABItem, dependList);
        }

        /// <summary>
        /// 获取依赖列表
        /// 主资源为最后一个
        /// </summary>
        /// <returns></returns>
        public (AssetBundleItem, IEnumerable<AssetBundleItem>) GetDependAssetsByGUID(string guid, Type type = null)
        {
            //获取主资源信息
            var mainABItem = GetAssetBundleItemByGUID(guid, type);
            //获取依赖列表
            var dependList = GetDependAssets(mainABItem);
            return (mainABItem, dependList);
        }

        /// <summary>
        /// 获取依赖文件
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AssetBundleItem> GetDependAssets(AssetBundleItem assetBundleItem)
        {
            List<AssetBundleItem> retlist = null;
            if (assetBundleItem != null && assetBundleItem.DependAssetIds != null && assetBundleItem.DependAssetIds.Length > 0)
            {
                int len = assetBundleItem.DependAssetIds.Length;
                retlist = new List<AssetBundleItem>(len);
                //找到依赖资源
                for (int i = 0; i < len; i++)
                {
                    var idx = assetBundleItem.DependAssetIds[i];

                    var abItem = this.AssetbundleItemList[idx];
                    retlist.Add(abItem);
                }
            }

            return retlist;
        }


        #region 获取Assetbundle文件本地

        /// <summary>
        /// 获取assetbundle的源文件
        /// </summary>
        public AssetBundleItem GetAssetBundleSourceFile(string loadPath)
        {
            var abItem = this.GetAssetBundleItem(loadPath);
            return this.GetAssetBundleSourceFile(abItem);
            return null;
        }

        /// <summary>
        /// 获取assetbundle的源文件
        /// </summary>
        public AssetBundleItem GetAssetBundleSourceFile(AssetBundleItem item)
        {
            if (item.IsAssetBundleSourceFile())
            {
                return item;
            }
            else if (item.DependAssetIds.Length > 0)
            {
                var idx = item.DependAssetIds[item.DependAssetIds.Length - 1];
                return this.GetAssetBundleItem(idx);
            }

            return null;
        }

        #endregion


#if UNITY_EDITOR

        /// <summary>
        /// 覆盖写入配置
        /// </summary>
        public void OverrideConfig()
        {
            var csv = CsvSerializer.SerializeToCsv(this.AssetbundleItemList);
            File.WriteAllText(this.configPath, csv);
        }
#endif
    }
}
