using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.ResourceMgr
{
    public class ManifestItem
    {
        public string Name = "null";
        public string UIID = "none";
        public List<string> Dependencies = new List<string>();
    }

    /// <summary>
    /// 配置文件
    /// </summary>
    public class ManifestConfig
    {
        private int count;
        private Dictionary<string, ManifestItem> manifestMap;

        /// <summary>
        /// json结构
        /// </summary>
        /// <param name="content"></param>
        public ManifestConfig(string content)
        {
            this.manifestMap = new Dictionary<string, ManifestItem>();
            var list = JsonMapper.ToObject<List<ManifestItem>>(content);

            foreach (var item in list)
            {
                this.manifestMap[item.Name] = item;
            }
        }

        public ManifestConfig()
        {
            this.manifestMap = new Dictionary<string, ManifestItem>();
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns></returns>
        public string[] GetDirectDependencies(string manifestName)
        {
            ManifestItem item = null;
            if (this.manifestMap.TryGetValue(manifestName, out item))
            {
                if (item == null)
                {
                    BDebug.LogError("资源为null:" + manifestName);
                }

                return item.Dependencies.ToArray();
            }

            return new string[0];
        }

        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="manifestName"></param>
        /// <returns></returns>
        public ManifestItem GetManifestItem(string manifestName)
        {
            ManifestItem item = new ManifestItem();
            this.manifestMap.TryGetValue(manifestName, out item);
            return item;
        }

        /// <summary>
        /// 添加一个依赖
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dependencies"></param>
        public void AddDepend(string name, string UIID, List<string> dependencies)
        {
            var item = new ManifestItem()
            {
                Name = name,
                UIID = UIID,
                Dependencies = dependencies
            };

            this.manifestMap[name] = item;
        }

        /// <summary>
        /// 获取所有的ab
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllAssetBundles()
        {
            List<string> list = new List<string>();
            //
            foreach (var value in this.manifestMap.Values)
            {
                list.AddRange(value.Dependencies);
            }

            return list.Distinct().ToList();
        }

        /// <summary>
        /// 转字符串
        /// </summary>
        /// <returns></returns>
        public string ToString()
        {
            this.count = manifestMap.Values.Count;

            
#if UNITY_EDITOR
            int i = 0;

            List<string> list = new List<string>();
            foreach (var v in manifestMap.Values)
            {
                list.AddRange(v.Dependencies);
            }

            var l = list.Distinct().ToList();
            Debug.Log(string.Format("<color=red>依赖数量:{0}</color>",l.Count));
#endif
            var items = manifestMap.Values.ToList();
            return JsonMapper.ToJson(items);
        }
    }

    /// <summary>
    /// manifest 
    /// </summary>
    public class AssetBundleManifestReference
    {
        /// <summary>
        /// 所有的assetbundle
        /// </summary>
        public HashSet<string> AssetBundlesSet { get; private set; }

        public ManifestConfig Manifest { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="path"></param>
        public AssetBundleManifestReference(string path)
        {
            if (File.Exists(path) == false)
            {
                Debug.LogError("manifest不存在:" + path);
                return;
            }

            //加载manifest
            var content = File.ReadAllText(path);
            //Debug.Log("content:"+content);
            this.Manifest = new ManifestConfig(content);
            BDebug.Log("manifest加载成功!");
            AssetBundlesSet = new HashSet<string>();
            var list = this.Manifest.GetAllAssetBundles();
            foreach (var l in list)
            {
                AssetBundlesSet.Add(l);
            }
        }
    }
}