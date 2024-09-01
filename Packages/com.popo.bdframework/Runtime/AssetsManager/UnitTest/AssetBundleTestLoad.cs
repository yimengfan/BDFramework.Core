using System;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetsManager.UnitTest
{
    public class AssetBundleTestLoad : MonoBehaviour
    {
        private void Awake()
        {
            //初始化
            var abPath = Application.isEditor ? BApplication.DevOpsPublishAssetsPath : Application.persistentDataPath;
            BResources.InitLoadAssetBundleEnv(abPath, BApplication.RuntimePlatform);
            BResources.ResLoader.WarmUpShaders();
        }

        public Transform Parent;
        public string Path;
        
        [HorizontalGroup("a")]
        [HorizontalGroup("a/a1")]
        [Button("同步加载")]
        public void Btn_Load()
        {
            var go = BResources.Load<Object>(this.Path);
            GameObject.Instantiate(go);

        }


        [HorizontalGroup("a/a1")]
        [Button("异步加载")]
        public void Async_Load()
        {
            BResources.AsyncLoad<GameObject>(this.Path, (go) =>
            {
                var ngo = GameObject.Instantiate(go, Parent);
            });
        }
    }
}
