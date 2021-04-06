using System;
using System.Collections;
using System.IO;
using LitJson;
using UnityEngine;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// manifest 
    /// </summary>
    public class ManifestLoder
    {
        /// <summary>
        /// 配置
        /// </summary>
        public ManifestConfig Manifest { get; private set; }

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onLoaded"></param>
        public void Load(string path)
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                this.Manifest = JsonMapper.ToObject<ManifestConfig>(content);
            }
            else
            {
                BDebug.LogError("配置文件不存在:" + path);
            }
        }
    }
}