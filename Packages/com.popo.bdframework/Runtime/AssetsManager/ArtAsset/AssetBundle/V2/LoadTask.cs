using System;
using UnityEngine;

namespace AssetsManager.ArtAsset.AssetBundle.V2
{
    public class LoadTask : CustomYieldInstruction
    {
        /// <summary>
        /// 实现协程的判断
        /// </summary>
        public override bool keepWaiting
        {
            get { return isDone; }
        }

        /// <summary>
        /// 是否为异步任务
        /// </summary>
        public bool IsAsyncTask { get; private set; } = false;
        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsDone
        {
            get
            {
                if (AssetBundleRequest != null)
                {
                    //异步任务的判断
                    return AssetBundleRequest.isDone;
                }
                else
                {
                    // 同步任务的判断
                    return isDone;
                }
            }
        }

        /// <summary>
        /// AB 对象
        /// </summary>
        public UnityEngine.AssetBundle AssetBundle { get; private set; }

        /// <summary>
        /// 是否完成,一般用以给同步
        /// </summary>
        private bool isDone = false;

        /// <summary>
        /// 是否开始
        /// </summary>
        private bool isStartLoading = false;

        /// <summary>
        /// 异步加载对象
        /// </summary>
        private AssetBundleCreateRequest AssetBundleRequest { get; set; }

        /// <summary>
        /// ab path
        /// </summary>
        public string LocalPath { get; private set; }

        private uint crc;
        private ulong offset;

        //构造
        public LoadTask(string localPath, uint crc, ulong offset)
        {
            this.LocalPath = localPath;
            this.crc = crc;
            this.offset = offset;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="path"></param>
        public void Load()
        {
            if (!this.isStartLoading)
            {
              
                this.isStartLoading = true;
                this.AssetBundle = UnityEngine.AssetBundle.LoadFromFile(LocalPath, crc, offset);
                this.isDone = true;
            }
            else
            {
                Debug.LogError("Task is loading!");
            }
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="path"></param>
        public void AysncLoad()
        {
            if (!this.isStartLoading)
            {
                this.IsAsyncTask = true;
                this.isStartLoading = true;
                this.AssetBundleRequest = UnityEngine.AssetBundle.LoadFromFileAsync(LocalPath, crc, offset);
            }
            else
            {
                Debug.LogError("Task is loading!");
            }
        }

        /// <summary>
        /// 异步任务转成同步
        /// </summary>
        /// <returns></returns>
        public bool ToSynchronizationTask()
        {
            if (AssetBundleRequest != null && !AssetBundleRequest.isDone)
            {
                // abRequest.assetBundle.Unload(true);
                //直接访问转为同步
                this.AssetBundle = AssetBundleRequest.assetBundle;
                
                return true;
            }

            return false;
        }
    }
}
