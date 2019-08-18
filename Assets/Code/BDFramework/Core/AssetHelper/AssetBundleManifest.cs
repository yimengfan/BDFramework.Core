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
        /// <summary>
        /// 资源名,单ab 单资源情况下. name = ab名
        /// </summary>
        public string Name = "null";
        public string UIID = "none";
        /// <summary>
        /// 单ab 多资源情况下，packagename 就是ab名 
        /// </summary>
        public string PackageName = "";
        public List<string> Dependencies = new List<string>();
    }

    /// <summary>
    /// 配置文件
    /// </summary>
    public class ManifestConfig
    {
        public Dictionary<string, ManifestItem> ManifestMap { get; private set; }

        /// <summary>
        /// json结构
        /// </summary>
        /// <param name="content"></param>
        public ManifestConfig(string content)
        {
            this.ManifestMap = new Dictionary<string, ManifestItem>();
            var list = JsonMapper.ToObject<List<ManifestItem>>(content);

            foreach (var item in list)
            {
                this.ManifestMap[item.Name] = item;
            }
        }

        public ManifestConfig()
        {
            this.ManifestMap = new Dictionary<string, ManifestItem>();
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns></returns>
        public string[] GetDirectDependencies(string manifestName)
        {
            ManifestItem item = null;
            if (this.ManifestMap.TryGetValue(manifestName, out item))
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
            if (!string.IsNullOrEmpty(manifestName))
            {
                ManifestItem item = new ManifestItem();
                this.ManifestMap.TryGetValue(manifestName, out item);
                return item;
            }

            return null;
        }

        /// <summary>
        /// 添加一个依赖
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dependencies"></param>
        public void AddDepend(string name, string UIID, List<string> dependencies ,string packageName = "")
        {

            var item = new ManifestItem()
            {
                Name = name,
                UIID = UIID,
                Dependencies = dependencies,
                PackageName =  packageName
            };

            if (this.ManifestMap.ContainsKey(name))
            {
                //prefab 嵌套的情况, 2018新系统
                //被依赖项 其实也有依赖，
                if (dependencies.Count >= this.ManifestMap[name].Dependencies.Count)
                {
                    this.ManifestMap[name] = item;
                }
            }
            else
            {
                this.ManifestMap[name] = item;
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
            foreach (var m in this.ManifestMap)
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
          

#if UNITY_EDITOR
            int i = 0;

            List<string> list = new List<string>();
            foreach (var v in ManifestMap.Values)
            {
                list.AddRange(v.Dependencies);
            }

            var l = list.Distinct().ToList();
            Debug.Log(string.Format("<color=red>依赖数量:{0}</color>", l.Count));
#endif
            var items = ManifestMap.Values.ToList();
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