using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
   
    /// <summary>
    /// manifest 
    /// </summary>
    public class AssetBundleManifestReference
    {
        /// <summary>
        /// 所有的assetbundle
        /// </summary>
        public HashSet<string> AssetBundlesSet;
        public AssetBundleManifest Manifest
        {
            get;
            private set;
        }

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
                var dd = DataDrivenServer.GetService("BDFrameLife");
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
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                path = "file:///" + path;
            }
                
            BDebug.Log("加载依赖");
            WWW www = new WWW(path);
            yield return www;
            if (www.error == null)
            {
                this.Manifest = www.assetBundle.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
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