using System;
using System.Collections;
using System.IO;
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
            //这里开个同步接口 为了单元测试用
            if (File.Exists(path))
            {
                BDebug.Log("manifest加载成功!");
                var text = File.ReadAllText(path);
                this.Manifest = new ManifestConfig(text);
            }
            else
            {
                Debug.LogError("配置文件不存在:" + path);
            }
        }
    }
}