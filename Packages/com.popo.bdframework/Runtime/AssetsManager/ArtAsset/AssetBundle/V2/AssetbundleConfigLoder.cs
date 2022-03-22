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
        public (AssetBundleItem, List<string>) GetDependAssets<T>(string assetName) where T : Object
        {
            return GetDependAssets(assetName, typeof(T));
        }

        /// <summary>
        /// 获取单个依赖
        /// Type版本
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="type"></param>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public (AssetBundleItem, List<string>) GetDependAssets(string assetName, Type type = null)
        {
            //获取资源信息
            var retABItem = GetAssetBundleData(assetName, type);
            //回去依赖列表
            var retList = GetDependAssets(retABItem);
            return (retABItem, retList);
            return (null, null);
        }


        /// <summary>
        /// 获取依赖文件
        /// </summary>
        /// <returns></returns>
        public List<string> GetDependAssets(AssetBundleItem assetBundleItem)
        {
            var retlist = new List<string>();
            if (assetBundleItem != null && assetBundleItem.DependAssetIds != null && assetBundleItem.DependAssetIds.Length > 0)
            {
                int len = assetBundleItem.DependAssetIds.Length;
                retlist = new List<string>(len);
                //找到依赖资源
                for (int i = 0; i < len; i++)
                {
                    var idx = assetBundleItem.DependAssetIds[i];
                    var abItem = this.AssetbundleItemList[idx];
                    retlist.Add(abItem.AssetBundlePath);
                }
            }


            return retlist;
        }


        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleData<T>(string assetLoadPath) where T : Object
        {
            return GetAssetBundleData(assetLoadPath, typeof(T));
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
        /// <param name="type"></param>
        /// <returns></returns>
        public AssetBundleItem GetAssetBundleData(string assetLoadPath, Type type = null)
        {
            if (!string.IsNullOrEmpty(assetLoadPath))
            {
                if (type != null && type != typeof(Object))
                {
                    //指定T Map搜索
                    if (this.assetTypeIdxMap.TryGetValue(type.FullName, out var assetMap))
                    {
                        if (assetMap.TryGetValue(assetLoadPath, out var idx))
                        {
                            return this.AssetbundleItemList[idx];
                        }
                    }
                }
                else
                {
                    //全局搜索,效率略低
                    for (int i = 0; i < this.AssetbundleItemList.Count; i++)
                    {
                        var abitem = this.AssetbundleItemList[i];
                        if (abitem.LoadPath != null)
                        {
                            if (abitem.LoadPath.Equals(assetLoadPath, StringComparison.OrdinalIgnoreCase))
                            {
                                return abitem;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
