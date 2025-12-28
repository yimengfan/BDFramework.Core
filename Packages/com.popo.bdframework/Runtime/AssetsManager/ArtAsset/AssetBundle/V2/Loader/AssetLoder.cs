using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    ///ab包装类
    /// </summary>
    public class AssetLoder
    {
        /// <summary>
        /// 当前Assetbundle
        /// </summary>
        protected AssetBundle AssetBundle { get; private set; }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name
        {
            get
            {
                return AssetBundle.name;
            }
        }

        public AssetLoder(AssetBundle ab)
        {
            this.AssetBundle = ab;
        }


        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid
        {
            get
            {
                return AssetBundle;
            }
        }
        
        /// <summary>
        /// 是否被卸载
        /// </summary>
        public bool IsUnload { get; private set; } = false;

        /// <summary>
        /// 缓存fullmap
        /// </summary>
        Dictionary<string, string> assetLocalNameCacheMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 特殊资源实例的缓存，实际用到时会很少
        /// </summary>
        protected Dictionary<string, Object> specialObjectCacheMap = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);



        /// <summary>
        /// 获取资源全路径
        /// </summary>
        /// <param name="loadPath">使用guid作为loadpath</param>
        /// <returns></returns>
        private string GetAssetLocalPath(string loadPath, LoadPathType loadPathType)
        {
            string assetLocalPath = "";

                var fs = this.AssetBundle.GetAllAssetNames();

                switch (loadPathType)
                {
                    //guid是全路径
                    case LoadPathType.GUID:
                    {
                        assetLocalPath = fs.FirstOrDefault((p) => p.Equals(loadPath, StringComparison.OrdinalIgnoreCase));
                    }
                        break;
                    //runtime是相对路径
                    case LoadPathType.RuntimePath:
                    {
                        // //有可能加载者输入了后缀名
                        // var ext = Path.GetExtension(loadPath);
                        // //无后缀
                        // if (string.IsNullOrEmpty(ext))
                        // {
                       
                        //这里忽略大小写的Contains消耗慢几十倍
#if UNITY_EDITOR
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
#endif
                        var subPath = ZString.Concat("/", loadPath, ".").ToLower();
                        assetLocalPath = fs.FirstOrDefault((p) => p.Contains(subPath));
#if UNITY_EDITOR
                        sw.Stop();
                        BDebug.Log($"寻找AB fs耗时: {sw.ElapsedTicks/10000f} ms  fs count:{fs.Length}",Color.magenta);
#endif                   
                        // }
                        // //有后缀
                        // else
                        // {
                        //     var subPath = ZString.Concat("/", loadPath);
                        //     assetLocalPath = fs.FirstOrDefault((p) => p.EndsWith(loadPath, StringComparison.OrdinalIgnoreCase));
                        // }
                    }
                        break;
                }


                if (assetLocalPath == null)
                {
                    BDebug.LogError("资产不存在:" + loadPath);
                }


            

            return assetLocalPath;
        }

      

        
        #region 引用计数

        /// <summary>
        /// 使用计数
        /// </summary>
        public int UseCounter { get; private set; }

        /// <summary>
        /// 使用
        /// </summary>
        public void Use()
        {
            UseCounter++;
        }

        /// <summary>
        /// 不使用
        /// </summary>
        public void Unuse()
        {
            UseCounter--;
        }

        #endregion

        #region 加、卸载
        /// <summary>
        /// 加载普通资源
        /// </summary>
        /// <param name="loadType"></param>
        /// <param name="loadPath">>assetbunld中的加载名，现阶段为guid</param>
        /// <returns></returns>
        public Object LoadAsset(Type loadType, string loadPath)
        {
            //通用加载
           // var assetLocalPath = GetAssetLocalPath(assetLoadPath, loadPathType);
            if (!string.IsNullOrEmpty(loadPath))
            {
                var @object = this.AssetBundle.LoadAsset(loadPath, loadType);
                return @object;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 加载普通资源
        /// </summary>
        /// <param name="loadType">加载类型</param>
        /// <param name="assetLoadPath">api加载路径</param>
        /// <param name="callback">加载完的回调</param>
        /// <param name="validConfigType">验证类型</param>
        /// <returns></returns>
        public void AsyncLoadAsset(Type loadType, string assetLoadPath, Action<Object> callback, int validConfigType = -1)
        {
            //通用加载
           // var assetLocalName = GetAssetLocalPath(assetLoadPath, loadPathType);
            var request = LoadAssetAsync(loadType,assetLoadPath);
            var requestAwaiter = request.GetAwaiter();
            requestAwaiter.OnCompleted(() =>
            {
                var @object = requestAwaiter.GetResult();
                callback?.Invoke(@object);
            });
        }
        /// <summary>
        /// 创建异步实例化任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AssetBundleRequest LoadAssetAsync<T>(string loadPath, bool isNeedGetLocalPathFormLoadPath = false) where T : Object
        {
            return this.LoadAssetAsync(typeof(T), loadPath);
        }

        /// <summary>
        /// 创建异步实例化任务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="loadPath">assetbunld中的加载名，现阶段为guid</param>
        /// <param name="isNeedGetLocalPathFormLoadPath"></param>
        /// <returns></returns>
        public AssetBundleRequest LoadAssetAsync(Type type, string loadPath, bool isNeedGetLocalPathFormLoadPath = false)
        {
            return this.AssetBundle.LoadAssetAsync(loadPath, type);
        }
        
        /// <summary>
        /// 卸载
        /// </summary>
        virtual public void UnLoad()
        {
            this.UseCounter = 0;
            this.AssetBundle.Unload(true);
            this.IsUnload = true;
        }

        #endregion


        /// <summary>
        /// 获取所有资源名
        /// </summary>
        /// <returns></returns>
        public string[] GetAllAssetNames()
        {
            return AssetBundle.GetAllAssetNames();
        }

    }
}
