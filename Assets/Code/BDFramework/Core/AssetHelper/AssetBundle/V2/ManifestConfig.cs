using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.ResourceMgr.V2
{


    /// <summary>
    /// 配置文件
    /// </summary>
    public class ManifestConfig
    {
        /// <summary>
        /// 资源Map
        /// </summary>
        public Dictionary<string, ManifestItem>
            ManifestMap { get; private set; } //= new Dictionary<string, ManifestItem>();

        /// <summary>
        /// json结构
        /// </summary>
        /// <param name="content"></param>
        public ManifestConfig(string content)
        {
            ManifestMap = JsonMapper.ToObject<Dictionary<string, ManifestItem>>(content);
        }

        /// <summary>
        /// 获取单个依赖
        /// </summary>
        /// <param name="menifestName"></param>
        /// <returns>这个list外部不要修改</returns>
        public List<string> GetDependenciesByName(string name)
        {
            ManifestItem item = null;
            if (this.ManifestMap.TryGetValue(name, out item))
            {
                var list = new List<string>(item.Depend);
                return list;
            }

            BDebug.LogError("【config】不存在资源:" + name);
            return null;
        }


        /// <summary>
        /// 获取单个menifestItem
        /// </summary>
        /// <param name="manifestName"></param>
        /// <returns></returns>
        public ManifestItem GetManifest(string manifestName)
        {
            if (!string.IsNullOrEmpty(manifestName))
            {
                ManifestItem item = null;
                this.ManifestMap.TryGetValue(manifestName, out item);
                return item;
            }

            return null;
        }


        /// <summary>
        /// 添加一个依赖,这个仅在Editor下调用
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dependencies"></param>
        public void AddItem(string path,
            string hash,
            List<string> dependencies,
            ManifestItem.AssetTypeEnum @enum,
            string abname = "")
        {
            ManifestItem item = null;
            if (this.ManifestMap.TryGetValue(path, out item))
            {
                if (item.Path != hash)
                {
                    Debug.LogError("有重名：" + path + ",如无显式BResource.Load加载则无视,隐式会自动根据hash算依赖!");
                    path = path + "_r";
                    item = new ManifestItem(path, abname, @enum, dependencies);
                    this.ManifestMap[path] = item;
                }
                else if (dependencies.Count >= item.Depend.Count)
                {
                    item = new ManifestItem(path, abname, @enum, dependencies);
                    this.ManifestMap[path] = item;
                }
            }
            else
            {
                item = new ManifestItem(path, abname, @enum, dependencies);
                this.ManifestMap[path] = item;
            }
        }


        public void AddItem(ManifestItem item)
        {
            if (this.ManifestMap.ContainsKey(item.Path))
            {
                //prefab 嵌套的情况, 2018新系统
                //被依赖项 其实也有依赖，
                if (item.Depend.Count >= this.ManifestMap[item.Path].Depend.Count)
                {
                    this.ManifestMap[item.Path] = item;
                }
            }
            else
            {
                this.ManifestMap[item.Path] = item;
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
            foreach (var v in ManifestMap.Values)
            {
                list.AddRange(v.Depend);
            }

            var l = list.Distinct().ToList();
#endif
            var items = ManifestMap.Values.ToList();
            return JsonMapper.ToJson(items);
        }
    }

}