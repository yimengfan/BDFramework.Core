using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Serialize;
using LitJson;
using ServiceStack.Text;
using UnityEngine;
using UnityEngine.U2D;
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
        #region

        /// <summary>
        /// Prefab
        /// </summary>
        public int TYPE_PREFAB = -1;

        /// <summary>
        /// 图集
        /// </summary>
        public int TYPE_SPRITE_ATLAS = -1;

        #endregion


        /// <summary>
        /// 是否为hash命名
        /// </summary>
        // public bool IsHashName { get; set; } = true;


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
        public AssetTypes AssetTypes { get; private set; }

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="onLoaded"></param>
        public void Load(string configPath, string assetTypePath)
        {
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
                var records = CsvSerializer.DeserializeFromString<List<AssetTypes>>(content);
                this.AssetTypes = records[0];
                //创建不同类型的映射表
                foreach (var assetType in this.AssetTypes.AssetTypeList)
                {
                    this.assetTypeIdxMap[assetType] = new LoadPathMap();
                }
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + configPath);
            }

            //资源配置
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
#if UNITY_EDITOR
                Stopwatch sw = new Stopwatch();
                sw.Start();
#endif
                var content = File.ReadAllText(configPath);
                this.AssetbundleItemList = CsvSerializer.DeserializeFromString<List<AssetBundleItem>>(content);
#if UNITY_EDITOR
                sw.Stop();
                BDebug.LogFormat("【AssetbundleV2】加载Config耗时{0}ms!", sw.ElapsedTicks / 10000L);
#endif


                foreach (var abItem in this.AssetbundleItemList)
                {
                    //可以被加载的资源
                    if (!string.IsNullOrEmpty(abItem.LoadPath) && this.AssetTypes != null)
                    {
                        var assettype = this.AssetTypes.AssetTypeList[abItem.AssetType];
                        var map = this.assetTypeIdxMap[assettype];
                        map[abItem.LoadPath] = abItem.Id;
                    }
                }
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + configPath);
            }

            //判断常用资源类型
            if (this.AssetTypes != null)
            {
                var typecls = typeof(GameObject).FullName;
                this.TYPE_PREFAB = this.AssetTypes.AssetTypeList.FindIndex((at) => at == typecls);
                typecls = typeof(SpriteAtlas).FullName;
                this.TYPE_SPRITE_ATLAS = this.AssetTypes.AssetTypeList.FindIndex((at) => at == typecls);
            }

            if (this.AssetTypes != null && this.AssetbundleItemList.Count > 0)
            {
                BDebug.Log("【AssetbundleV2】资源加载初始化完成,资源数量:" + this.AssetbundleItemList.Count);
            }
        }


        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<string>) GetDependAssetsByName<T>(string assetName) where T : Object
        {
            return GetDependAssetsByName(typeof(T), assetName);
        }

        /// <summary>
        /// 获取单个依赖
        /// Type版本
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<string>) GetDependAssetsByName(Type type, string assetName)
        {
            //1.优先用类型匹配
            var assetbundleItem = GetAssetBundleData(type, assetName);
            if (assetbundleItem != null)
            {
                var retlist = new List<string>(assetbundleItem.DependAssetIds.Count);
                //找到依赖资源
                for (int i = 0; i < assetbundleItem.DependAssetIds.Count; i++)
                {
                    var idx = assetbundleItem.DependAssetIds[i];
                    var abItem = this.AssetbundleItemList[idx];
                    retlist.Add(abItem.AssetBundlePath);
                }

                return (assetbundleItem, retlist);
            }
            //2.强行搜路径匹配即可
            else
            {
                return GetDependAssetsByName(assetName);
            }
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
                if (abitem.LoadPath != null)
                {
                    if (abitem.LoadPath.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                    {
                        retABItem = abitem;
                        break;
                    }
                }
            }

            if (retABItem != null)
            {
                var retlist = new List<string>(retABItem.DependAssetIds.Count);
                //找到依赖资源
                for (int i = 0; i < retABItem.DependAssetIds.Count; i++)
                {
                    var idx = retABItem.DependAssetIds[i];
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
            return GetAssetBundleData(typeof(T), assetLoadPath);
        }


        public Dictionary<string, AssetBundleItem> guildAssetBundleItemCahceMap = new Dictionary<string, AssetBundleItem>();

        /// <summary>
        /// 获取assetbunldeItem通过guid
        /// </summary>
        /// <param name="type"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleDataByGUID(string guid)
        {
            var ret = guildAssetBundleItemCahceMap.TryGetValue(guid, out var assetBundleItem);
            if (!ret)
            {
                //默认是assetbunlepath = meta guid
                assetBundleItem = this.AssetbundleItemList.Find((abi) => abi.AssetBundlePath.Equals(guid));
                guildAssetBundleItemCahceMap[guid] = assetBundleItem;
            }
            return assetBundleItem;
        }

        /// <summary>
        /// 获取单个menifestItem
        /// Type版本
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleData(Type type, string assetLoadPath)
        {
            if (!string.IsNullOrEmpty(assetLoadPath))
            {
                if (this.assetTypeIdxMap.TryGetValue(type.FullName, out var assetMap))
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