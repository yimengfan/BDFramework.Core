using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public void Load(string platformPath)
        {
            string assetsInfoPath = IPath.Combine(platformPath, BResources.ART_ASSET_INFO_PATH);
            ;
            string assetTypePath = IPath.Combine(platformPath, BResources.ART_ASSET_TYPES_PATH);
            ;
            //资源类型配置
            if (!string.IsNullOrEmpty(assetTypePath) && File.Exists(assetTypePath))
            {
                var content = File.ReadAllText(assetTypePath);
                // var list = CsvSerializer.DeserializeFromString<List<string>>(content);
                // var wlist = new List<AssetTypes>()
                // {
                //     new AssetTypes()
                //     {
                //         AssetTypeList = list,
                //     }
                // };
                // var str = CsvSerializer.SerializeToCsv(wlist);
                // File.WriteAllText(assetTypePath, str);
                // //
                // content = File.ReadAllText(assetTypePath);
                var records = CsvSerializer.DeserializeFromString<List<AssetTypeConfig>>(content);
                this.AssetTypes = records[0];
                //创建不同类型的映射表
                foreach (var assetType in this.AssetTypes.AssetTypeList)
                {
                    this.assetTypeABIdxMap[assetType] = new LoadPathIdxMap();
                }
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + assetsInfoPath);
            }

            //资源配置
            if (!string.IsNullOrEmpty(assetsInfoPath) && File.Exists(assetsInfoPath))
            {
                this.configPath = assetsInfoPath;
#if UNITY_EDITOR
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif
                var content = File.ReadAllText(assetsInfoPath);
                this.AssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(content);
#if UNITY_EDITOR
                sw.Stop();
                BDebug.LogFormat("【AssetbundleV2】加载Config耗时{0}ms!", sw.ElapsedTicks / 10000L);
#endif


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
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + assetsInfoPath);
            }

            //初始化常用资源类型
            // if (this.AssetTypes != null)
            // {
            //     //Prefab
            //     var clsName = typeof(GameObject).FullName;
            //     AssetType.VALID_TYPE_PREFAB = this.AssetTypes.AssetTypeList.FindIndex((at) => at.Equals(clsName, StringComparison.OrdinalIgnoreCase));
            //     //图集
            //     clsName = typeof(SpriteAtlas).FullName;
            //     AssetType.VALID_TYPE_SPRITE_ATLAS = this.AssetTypes.AssetTypeList.FindIndex((at) => at.Equals(clsName, StringComparison.OrdinalIgnoreCase));
            //     //...
            //     //其他省略，需要时候再加
            // }


            BDebug.Log("【AssetbundleV2】资源加载初始化完成,资源总量:" + this.AssetbundleItemList?.Count);
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
#if UNITY_EDITOR
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif
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
#if UNITY_EDITOR
                sw.Stop();
                BDebug.Log($"寻找ABItem耗时: {sw.ElapsedTicks / 10000f} ms - {guid}", "yellow");
#endif
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
        /// 获取单个menifestItem
        /// Type版本
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleItem(string loadPath, Type type = null, bool searchByGuid = false)
        {
            AssetBundleItem assetBundleItem = null;
            if (!string.IsNullOrEmpty(loadPath))
            {
                BDebug.LogWatchBegin($"寻找ABItem-{loadPath}");
                if (type == null || type == typeof(Object))
                {
                    //全局搜索,效率略低
                    for (int i = 0; i < this.AssetbundleItemList.Count; i++)
                    {
                        var abitem = this.AssetbundleItemList[i];
                       
                        if (searchByGuid && abitem.GUID == loadPath)
                        {
                            assetBundleItem = abitem;
                            break;
                        }
                        else  if (abitem.LoadPath != null && abitem.LoadPath.Equals(loadPath, StringComparison.OrdinalIgnoreCase))
                        {
                            assetBundleItem = abitem;
                            break;
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

                BDebug.LogWatchEnd($"寻找ABItem-{loadPath}","green");

            }

            return assetBundleItem;
        }


        /// <summary>
        /// 获取依赖列表
        /// 主资源为最后一个
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<AssetBundleItem>) GetDependAssets<T>(string assetLoadPath) where T : Object
        {
            return GetDependAssets(assetLoadPath, typeof(T));
        }

        /// <summary>
        /// 获取依赖列表
        /// 主资源为最后一个
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <param name="type"></param>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<AssetBundleItem>) GetDependAssets(string assetLoadPath, Type type = null)
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
        /// <param name="assetLoadPath"></param>
        /// <param name="type"></param>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<AssetBundleItem>) GetDependAssetsByGUID(string guid, Type type = null)
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
        public List<AssetBundleItem> GetDependAssets(AssetBundleItem assetBundleItem)
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