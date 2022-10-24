using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BDFramework.StringEx;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    ///ab包装类
    /// </summary>
    public class AssetBundleWrapper
    {
        /// <summary>
        /// 当前Assetbundle
        /// </summary>
        public AssetBundle AssetBundle { get; private set; }


        public AssetBundleWrapper(AssetBundle ab)
        {
            this.AssetBundle = ab;
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
        /// 资源实例的缓存，实际用到时会很少
        /// </summary>
        private Dictionary<string, Object> specialObjectCacheMap = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 所有的shader
        /// </summary>
        private List<Shader> shaders = null;

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
                        BDebug.Log($"寻找AB fs耗时: {sw.ElapsedTicks/10000f} ms  fs count:{fs.Length}","pink");
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

        /// <summary>
        /// 创建异步实例化任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AssetBundleRequest CreateAsyncInstantiateTask<T>(string loadPath, bool isNeedGetLocalPathFormLoadPath = false) where T : Object
        {
            return this.CreateAsyncInstantiateTask(typeof(T), loadPath);
        }

        /// <summary>
        /// 创建异步实例化任务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="loadPath"></param>
        /// <param name="isNeedGetLocalPathFormLoadPath"></param>
        /// <returns></returns>
        public AssetBundleRequest CreateAsyncInstantiateTask(Type type, string loadPath, bool isNeedGetLocalPathFormLoadPath = false)
        {
            // if (isNeedGetLocalPathFormLoadPath)
            // {
            //     loadPath = GetAssetLocalPath(loadPath, loadPathType);
            // }

            return this.AssetBundle.LoadAssetAsync(loadPath, type);
        }

        #region 各种加载接口

        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        private SpriteAtlas LoadAtlas()
        {
            //默认一个ab中只有一个atlas
            var atlasName = AssetBundle.GetAllAssetNames().FirstOrDefault((a) => a.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase));

            //优先使用缓存
            specialObjectCacheMap.TryGetValue(atlasName, out var @object);
            SpriteAtlas atlas = null;
            if (@object)
            {
                atlas = @object as SpriteAtlas;
            }
            else
            {
                //不存在缓存 进行加载
                atlas = this.AssetBundle.LoadAsset<SpriteAtlas>(atlasName);
                specialObjectCacheMap[atlasName] = atlas;
            }

            return atlas;
        }


        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        private Object LoadSpriteFormAtlas(string texName)
        {
            var atlas = LoadAtlas();
            if (atlas)
            {
                texName = Path.GetFileName(texName);
                var sp = atlas.GetSprite(texName);
                return sp;
            }
            else
            {
                return null;
            }
        }

        #region 废弃代码

        // /// <summary>
        // /// 加载图集资源,仅支持 unity atlas方案
        // /// </summary>
        // /// <param name="texPath"></param>
        // /// <returns></returns>
        // private void AsyncLoadAtlas(Action<SpriteAtlas> callback)
        // {
        //     //默认一个ab中只有一个atlas
        //     var atlasName = AssetBundle.GetAllAssetNames().FirstOrDefault((a) => a.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase));
        //     specialObjectCacheMap.TryGetValue(atlasName, out var @object);
        //     if (@object)
        //     {
        //         var atlas = @object as SpriteAtlas;
        //         if (atlas)
        //         {
        //             callback?.Invoke(atlas);
        //         }
        //     }
        //     else
        //     {
        //         //开始加载
        //         var request = CreateAsyncInstantiateTask<SpriteAtlas>(atlasName);
        //         var requestAwaiter = request.GetAwaiter();
        //         //完成回调
        //         requestAwaiter.OnCompleted(() =>
        //         {
        //             //获取图集
        //             var atlas = requestAwaiter.GetResult() as SpriteAtlas;
        //             //缓存
        //             specialObjectCacheMap[atlasName] = atlas;
        //             if (atlas)
        //             {
        //                 callback?.Invoke(atlas);
        //             }
        //         });
        //     }
        // }
        //
        // /// <summary>
        // /// 加载图集资源,仅支持 unity atlas方案
        // /// </summary>
        // /// <param name="texPath"></param>
        // /// <returns></returns>
        // private void AsyncLoadSpriteFormAtlas(string texPath, Action<Object> callback)
        // {
        //     AsyncLoadAtlas((atlas) =>
        //     {
        //         //加载sp
        //         texPath = Path.GetFileName(texPath);
        //         var sp = atlas.GetSprite(texPath);
        //         callback?.Invoke(sp);
        //     });
        // }

        #endregion



        /// <summary>
        /// 加载所有shader
        /// </summary>
        public void LoadAllShaders()
        {
            //获取shader
            var objects = this.AssetBundle.LoadAllAssets<Shader>();
            this.shaders = new List<Shader>(objects);
        }

        /// <summary>
        /// 获取shader
        /// </summary>
        /// <returns></returns>
        public Shader FindShader(string shaderName)
        {
            var shader = this.shaders.FirstOrDefault((s) => s.name.Equals(shaderName, StringComparison.OrdinalIgnoreCase));
            return shader;
        }

        /// <summary>
        /// 加载普通资源
        /// </summary>
        /// <param name="loadType"></param>
        /// <param name="assetLoadPath">api加载路径</param>
        /// <returns></returns>
        public Object LoadAsset(Type loadType, string assetLoadPath)
        {
            //通用加载
           // var assetLocalPath = GetAssetLocalPath(assetLoadPath, loadPathType);
            if (!string.IsNullOrEmpty(assetLoadPath))
            {
                var @object = this.AssetBundle.LoadAsset(assetLoadPath, loadType);
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
            var request = CreateAsyncInstantiateTask(loadType,assetLoadPath);
            var requestAwaiter = request.GetAwaiter();
            requestAwaiter.OnCompleted(() =>
            {
                var @object = requestAwaiter.GetResult();
                callback?.Invoke(@object);
            });
        }

        #endregion

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

        /// <summary>
        /// 卸载
        /// </summary>
        public void UnLoad()
        {
            this.UseCounter = 0;
            this.AssetBundle.Unload(true);
            this.IsUnload = true;
        }
    }
}
