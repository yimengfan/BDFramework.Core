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
        /// 成功回调
        /// </summary>
        private Action onLoaded { get; set; }

        /// <summary>
        /// 配置
        /// </summary>
        public ManifestConfig Manifest { get; private set; }

        /// <summary>
        /// 加载接口
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onLoaded"></param>
        public void Load(string path, Action onLoaded)
        {
            this.onLoaded = onLoaded;

            if (Application.isEditor)
            {
                //这里开个同步接口 为了单元测试用
                if (File.Exists(path))
                {
                    BDebug.Log("manifest加载成功!");
                    var text = File.ReadAllText(path);
                    this.Manifest = new ManifestConfig(text);
                    this.onLoaded?.Invoke();
                    this.onLoaded = null;
                }
                else
                {
                    Debug.LogError("配置文件不存在:" + path);
                }
            }
            else
            {
                //加载manifest
                IEnumeratorTool.StartCoroutine(IE_LoadConfig(path));
            }
        }


        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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

            if (!string.IsNullOrEmpty(text))
            {
                this.Manifest = new ManifestConfig(text);
                BDebug.Log("manifest加载成功!");
                onLoaded?.Invoke();
                onLoaded = null;
            }
        }
    }
}