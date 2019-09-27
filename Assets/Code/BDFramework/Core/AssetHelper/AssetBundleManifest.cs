using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.ResourceMgr
{
    public class ManifestItem
    {
        public enum AssetTypeEnum
        {
            Others = 0,
            Prefab,
            TextAsset,
            Texture,
            SpriteAtlas,
        }

        public ManifestItem(string name, string hash, string package, AssetTypeEnum @enum,
            List<string> Depend = null)
        {
            this.Name = name;
            this.Hash = hash;
            this.Package = package;
            this.Type = (int) @enum;
            if (Depend == null) Depend = new List<string>();
            this.Depend = Depend;
        }

        public ManifestItem()
        {
        }

        /// <summary>
        /// 资源名,单ab 单资源情况下. name = ab名
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// hash
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// 单ab 多资源情况下，packagename 就是ab名 
        /// </summary>
        public string Package { get; private set; }

        /// <summary>
        /// asset类型
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// 依赖
        /// </summary>
        public List<string> Depend { get; set; } = new List<string>();
    }

    /// <summary>
    /// 配置文件
    /// </summary>
    public class ManifestConfig
    { 
        public Dictionary<string, ManifestItem> Manifest_NameKey { get;  set; } =
            new Dictionary<string, ManifestItem>();

        public Dictionary<string, ManifestItem> Manifest_HashKey { get;  set; } =
            new Dictionary<string, ManifestItem>();

        /// <summary>
        /// json结构
        /// </summary>
        /// <param name="content"></param>
        public ManifestConfig(string content)
        {
            var list = JsonMapper.ToObject<List<ManifestItem>>(content);
            foreach (var item in list)
            {
                this.Manifest_NameKey[item.Name] = item;
                this.Manifest_HashKey[item.Hash] = item;
            }
        }

        public ManifestConfig()
        {
            this.Manifest_NameKey = new Dictionary<string, ManifestItem>();
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns></returns>
        public string[] GetDirectDependenciesByName(string name)
        {
            ManifestItem item = null;
            if (this.Manifest_NameKey.TryGetValue(name, out item))
            {
                var list = new List<string>(item.Depend);
                list.Add(item.Hash);
                return list.ToArray();
            }
            BDebug.LogError("【config】不存在资源:"+name);
            return null;
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns></returns>
        public string[] GetDirectDependenciesByHash(string hash)
        {
            ManifestItem item = null;
            if (this.Manifest_HashKey.TryGetValue(hash, out item))
            {
                var list = new List<string>(item.Depend);
                list.Add(item.Hash);
                return list.ToArray();
            }
            
            BDebug.LogError("【config】不存在资源:"+hash);
            return null;
        }

        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="manifestName"></param>
        /// <returns></returns>
        public ManifestItem GetManifestItemByName(string manifestName)
        {
            if (!string.IsNullOrEmpty(manifestName))
            {
                ManifestItem item = null;
                this.Manifest_NameKey.TryGetValue(manifestName, out item);
                return item;
            }

            return null;
        }

        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="manifestName"></param>
        /// <returns></returns>
        public ManifestItem GetManifestItemByHash(string hashName)
        {
            if (!string.IsNullOrEmpty(hashName))
            {
                ManifestItem item = null;
                this.Manifest_HashKey.TryGetValue(hashName, out item);
                return item;
            }

            return null;
        }

        /// <summary>
        /// 添加一个依赖,这个仅在Editor下调用
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dependencies"></param>
        public void AddItem(string name, string hash, List<string> dependencies, ManifestItem.AssetTypeEnum @enum, string packageName = "")
        {
            
           
            ManifestItem item = null;
            if (this.Manifest_NameKey.TryGetValue(name,out item))
            {
                if (item.Hash!=hash)
                {
                    Debug.LogError("有重名：" +name+",如无显式BResource.Load加载则无视,隐式会自动根据hash算依赖!" );
                    name = name + "_r";
                    item = new ManifestItem(name, hash, packageName, @enum,dependencies);
                    this.Manifest_NameKey[name] = item;
                    this.Manifest_HashKey[hash] = item;
                }
                else if (dependencies.Count >=  item.Depend.Count)
                {
                    item = new ManifestItem(name, hash, packageName, @enum,dependencies);
                    this.Manifest_NameKey[name] = item;
                    this.Manifest_HashKey[hash] = item;
                }
            }
            else
            { 
                item = new ManifestItem(name, hash, packageName, @enum,dependencies);
                this.Manifest_NameKey[name] = item;
                this.Manifest_HashKey[hash] = item;
            }
        }


        public void AddItem(ManifestItem item)
        {
            if (this.Manifest_NameKey.ContainsKey(item.Name))
            {
                //prefab 嵌套的情况, 2018新系统
                //被依赖项 其实也有依赖，
                if (item.Depend.Count >= this.Manifest_NameKey[item.Name].Depend.Count)
                {
                    this.Manifest_NameKey[item.Name] = item;
                    this.Manifest_HashKey[item.Hash] = item;
                }
            }
            else
            {
                this.Manifest_NameKey[item.Name] = item;
                this.Manifest_HashKey[item.Hash] = item;
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
            foreach (var m in this.Manifest_NameKey)
            {
                //添加主体
                list.Add(m.Key);
                //添加依赖
                list.AddRange(m.Value.Depend);
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
            foreach (var v in Manifest_NameKey.Values)
            {
                list.AddRange(v.Depend);
            }

            var l = list.Distinct().ToList();
#endif
            var items = Manifest_NameKey.Values.ToList();
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
                    BDebug.Log("manifest加载失败!   ->" + path, "red");
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