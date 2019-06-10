using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
        public Dictionary<string, ManifestItem> Manifest { get; private set; }

        /// <summary>
        /// json结构
        /// </summary>
        /// <param name="content"></param>
        public ManifestConfig(string content)
        {
            this.Manifest = new Dictionary<string, ManifestItem>();
            var list = JsonMapper.ToObject<List<ManifestItem>>(content);

            foreach (var item in list)
            {
                this.Manifest[item.Name] = item;
            }
        }

        public ManifestConfig()
        {
            this.Manifest = new Dictionary<string, ManifestItem>();
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns></returns>
        public string[] GetDirectDependencies(string manifestName)
        {
            ManifestItem item = null;
            if (this.Manifest.TryGetValue(manifestName, out item))
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
            this.Manifest.TryGetValue(manifestName, out item);
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

            if (this.Manifest.ContainsKey(name))
            {
                //prefab 嵌套的情况, 2018新系统
                //被依赖项 其实也有依赖，
                if (dependencies.Count >= this.Manifest[name].Dependencies.Count)
                {
                    this.Manifest[name] = item;
                }
            }
            else
            {
                this.Manifest[name] = item;
            }
        }

        /// <summary>
        /// 获取所有的ab
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllAssetBundles()
        {
            List<string> list = new List<string>();
            //
            foreach (var m in this.Manifest)
            {
                //添加主体
                list.Add(m.Key);
                //添加依赖
                list.AddRange(m.Value.Dependencies);
            }

            return list.Distinct().ToList();
        }

        /// <summary>
        /// 转字符串
        /// </summary>
        /// <returns></returns>
        public string ToString()
        {
            this.count = Manifest.Values.Count;


#if UNITY_EDITOR
            int i = 0;

            List<string> list = new List<string>();
            foreach (var v in Manifest.Values)
            {
                list.AddRange(v.Dependencies);
            }

            var l = list.Distinct().ToList();
            Debug.Log(string.Format("<color=red>依赖数量:{0}</color>", l.Count));
#endif
            var items = Manifest.Values.ToList();
            return JsonMapper.ToJson(items);
        }
    }

    /// <summary>
    /// manifest 
    /// </summary>
    public class AssetBundleManifestReference
    {
        //
        public Action OnLoaded { get; set; }

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
            //加载manifest
            IEnumeratorTool.StartCoroutine(IE_LoadConfig(path));
        }


        private IEnumerator IE_LoadConfig(string path)
        {

            string text = "";

            if (File.Exists(path))
            {
                text = File.ReadAllText(path);
            }
            else
            {
                var www = new WWW(path);
                yield return www;
                if (www.isDone && www.error == null)
                {
                    text = www.text;
                }
                else
                {
                    BDebug.Log("manifest加载失败!   ->" + path ,"red");
                }
            }

            if (text != "")
            {
                this.Manifest = new ManifestConfig(text);
                BDebug.Log("manifest加载成功!");
                AssetBundlesSet = new HashSet<string>();
                var list = this.Manifest.GetAllAssetBundles();
                foreach (var l in list)
                {
                    AssetBundlesSet.Add(l);
                }

                //回调
                if (OnLoaded != null)
                {
                    OnLoaded();
                    OnLoaded = null;
                }
            }

            yield break;
        }
    }
}