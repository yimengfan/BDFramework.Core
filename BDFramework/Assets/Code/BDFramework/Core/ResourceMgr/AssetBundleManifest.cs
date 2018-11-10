using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LitJson;
using UnityEngine;

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
            return list;
        }
        /// <summary>
        /// 转字符串
        /// </summary>
        /// <returns></returns>
        public string ToString()
        {
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
        public HashSet<string> AssetBundlesSet;

        public ManifestConfig Manifest { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="path"></param>
        public AssetBundleManifestReference(string path)
        {
            //加载manifest
            IEnumeratorTool.StartCoroutine(IELoadManifest(path, b =>
            {
                if (b)
                {
                    BDebug.Log("manifest加载成功!");
                    AssetBundlesSet = new HashSet<string>();
                    var list = this.Manifest.GetAllAssetBundles();
                    foreach (var l in list)
                    {
                        AssetBundlesSet.Add(l);
                    }
                }
                else
                {
                    BDebug.LogError("manifest加载失败!");
                }

                //通知到BDFrameLife
                var dd = DataListenerServer.GetService("BDFrameLife");
                dd.TriggerEvent("OnAssetBundleOever");
            }));
        }

        /// <summary>
        /// 加载menifest 主配置文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="isManiFest"></param>
        /// <returns></returns>
        IEnumerator IELoadManifest(string path, Action<bool> callback)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                path = "file:///" + path;
            }

            BDebug.Log("加载依赖:" +path);
            WWW www = new WWW(path);
            yield return www;
            if (www.error == null)
            {
                //配置文件
                this.Manifest = new ManifestConfig(www.text);
                if (Manifest != null)
                {
                    callback(true);
                }
                else
                {
                    callback(false);
                }
            }
            else
            {
                Debug.LogError("错误：" + www.error);
            }
        }
    }
}