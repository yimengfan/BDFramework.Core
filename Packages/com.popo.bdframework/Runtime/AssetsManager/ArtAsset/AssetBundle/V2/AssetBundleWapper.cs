using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BDFramework.StringEx;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    ///ab包装类
    /// </summary>
    public class AssetBundleWapper
    {
        /// <summary>
        /// 当前Assetbundle
        /// </summary>
        public AssetBundle AssetBundle { get; private set; }


        public AssetBundleWapper(AssetBundle ab)
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
        /// 获取资源全路径
        /// </summary>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        private string GetAssetLocalPath(string loadPath)
        {
            string assetLocalPath = "";
            if (!assetLocalNameCacheMap.TryGetValue(loadPath, out assetLocalPath))
            {
                var fs = this.AssetBundle.GetAllAssetNames();
                if (fs.Length == 1)
                {
                    assetLocalPath = fs[0];
                }
                else
                {
                    var subPath = ZString.Concat("/", loadPath, ".");
                    assetLocalPath = fs.FirstOrDefault((p) => p.Contains(subPath, StringComparison.OrdinalIgnoreCase));
                }

                assetLocalNameCacheMap[loadPath] = assetLocalPath;
            }

            return assetLocalPath;
        }

        /// <summary>
        /// 创建异步实例化任务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AssetBundleRequest CreateAsyncInstantiateTask<T>(string path,bool isNeedGetLocalPathFormLoadPath =false) where T : Object
        {
            return this.CreateAsyncInstantiateTask(typeof(T), path,isNeedGetLocalPathFormLoadPath);
        }

        public AssetBundleRequest CreateAsyncInstantiateTask(Type type, string path,bool isNeedGetLocalPathFormLoadPath =false)
        {
            if (isNeedGetLocalPathFormLoadPath)
            {
                path = GetAssetLocalPath(path);
            }
            
            return this.AssetBundle.LoadAssetAsync(path, type);
        }

        #region 各种加载接口

        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        private Object LoadTextureFormAtlas(string texName)
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
            //不存在缓存 进行加载
            if (!atlas)
            {
                atlas = this.AssetBundle.LoadAsset<SpriteAtlas>(atlasName);
            }
            
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

        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texPath"></param>
        /// <returns></returns>
        private void AsyncLoadTextureFormAtlas(string texPath, Action<Object> callback)
        {
            //默认一个ab中只有一个atlas
            var atlasName = AssetBundle.GetAllAssetNames().FirstOrDefault((a) => a.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase));
            specialObjectCacheMap.TryGetValue(atlasName, out var @object);
            if (@object)
            {
                var atlas = @object as SpriteAtlas;
                if (atlas)
                {
                    //加载sp
                    texPath = Path.GetFileName(texPath);
                    var sp = atlas.GetSprite(texPath);
                    callback?.Invoke(sp);
                }
            }
            else
            {
                //开始加载
                var request = CreateAsyncInstantiateTask<SpriteAtlas>(atlasName);
                var requestAwaiter = request.GetAwaiter();
                //完成回调
                requestAwaiter.OnCompleted(() =>
                {
                    //获取图集
                    var atlas = requestAwaiter.GetResult() as SpriteAtlas;
                    if (atlas)
                    {
                        //缓存
                        specialObjectCacheMap[atlasName] = atlas;
                        //加载sp
                        texPath = Path.GetFileName(texPath);
                        var sp = atlas.GetSprite(texPath);
                        callback?.Invoke(sp);
                    }
                });
            }
        }


        /// <summary>
        /// 加载普通资源
        /// </summary>
        /// <param name="loadType"></param>
        /// <param name="assetLoadPath">api加载路径</param>
        /// <param name="validCofigType">打包时写入的类型,</param>
        /// <returns></returns>
        public Object LoadAsset(Type loadType, string assetLoadPath, int validCofigType = -1)
        {
            if (validCofigType != -1)
            {
                //图集特殊处理
                if (validCofigType == AssetType.VALID_TYPE_SPRITE_ATLAS)
                {
                    return LoadTextureFormAtlas(assetLoadPath);
                }
            }

            //通用加载
            var assetLocalPath = GetAssetLocalPath(assetLoadPath);
            Object @object = this.AssetBundle.LoadAsset(assetLocalPath, loadType);
            return @object;
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
            if (validConfigType != -1)
            {
                //图集特殊处理
                if (validConfigType == AssetType.VALID_TYPE_SPRITE_ATLAS)
                {
                    AsyncLoadTextureFormAtlas(assetLoadPath, callback);
                    return;
                }
            }

            //通用加载
            var assetLocalName = GetAssetLocalPath(assetLoadPath);
            var request = CreateAsyncInstantiateTask(loadType, assetLocalName);
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
