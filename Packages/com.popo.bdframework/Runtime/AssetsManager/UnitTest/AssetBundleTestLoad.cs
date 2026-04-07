using System;
using System.IO;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetsManager.UnitTest
{
    public class AssetBundleTestLoad : MonoBehaviour
    {
        private void Start()
        {
            //初始化
            if (Application.isEditor)
            {
                var abRootPath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetRuntimePlatformPath());
                BResources.InitLoadAssetBundleEnv(abRootPath, BApplication.RuntimePlatform);
            }
            else
            {
                throw new Exception("暂不支持真机测试");
            }


            BResources.ResLoader.WarmUpShaders();
        }

        public Transform Parent;
        public string Path;

        [HorizontalGroupAttribute("a")]
        [HorizontalGroupAttribute("a/a1")]
        [ButtonAttribute("同步加载")]
        public void Btn_Load()
        {
            var go = BResources.Load<Object>(this.Path);
            GameObject.Instantiate(go);
        }


        [HorizontalGroupAttribute("a/a1")]
        [ButtonAttribute("异步加载")]
        public void Async_Load()
        {
            BResources.AsyncLoad<GameObject>(this.Path, (go) =>
            {
                var ngo = GameObject.Instantiate(go, Parent);
            });
        }
    }
}
