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
        /// 版本
        /// </summary>
        public string Version = "1.0.0";
        /// <summary>
        /// 密钥
        /// </summary>
        public string AES = "";
        /// <summary>
        /// 资源Map
        /// </summary>
        public Dictionary<string, ManifestItem> ManifestMap { get; private set; } =new Dictionary<string, ManifestItem>();
        
        /// <summary>
        /// 添加manifest
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        public void AddManifest(string key, ManifestItem item)
        {
            ManifestMap[key] = item;
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
    }
}