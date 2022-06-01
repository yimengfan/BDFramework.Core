using System;
using BDFramework.ResourceMgr.V2;

namespace AssetsManager.ArtAsset.AssetBundle.V2
{
    /// <summary>
    /// 卸载任务
    /// </summary>
    public class UnLoadTask
    {
        /// <summary>
        /// 是否取消
        /// </summary>
        public bool IsCancel { get; private set; }

        /// <summary>
        /// 是否已经卸载
        /// </summary>
        public bool IsUnLoaded { get; private set; } = false;

        /// <summary>
        /// ab包装
        /// </summary>
        private AssetBundleWapper AssetBundleWapper;

        /// <summary>
        /// 完成回调
        /// </summary>
        private Action onUnloadEnd = null;
        /// <summary>
        /// 卸载
        /// </summary>
        /// <param name="unloadAction"></param>
        public UnLoadTask(AssetBundleWapper abw,Action onUnloadEnd =null)
        {
            this.AssetBundleWapper = abw;
            this.onUnloadEnd = onUnloadEnd;
        }

        /// <summary>
        /// 执行卸载
        /// </summary>
        public void Unload()
        {
            if (!IsCancel)
            {
                this.IsUnLoaded = true;
                this.AssetBundleWapper?.UnLoad();
                this.onUnloadEnd?.Invoke();
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public void Cancel()
        {
            IsCancel = true;
        }
    }
}
