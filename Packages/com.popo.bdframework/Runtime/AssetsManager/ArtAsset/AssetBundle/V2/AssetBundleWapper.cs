using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.StringEx;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    ///ab包引用计数类
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


        #region 各种加载接口

        Dictionary<string, string> assetNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        public Object LoadTextureFormAtlas(string texName)
        {
            //默认一个ab中只有一个atlas,
            var allAssetNames = AssetBundle.GetAllAssetNames();
            var atlas = this.AssetBundle.LoadAsset<SpriteAtlas>(allAssetNames.Last());
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
        /// 加载普通资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Object LoadAsset(Type type, string name)
        {
            string fullName = "";
            if (!assetNameMap.TryGetValue(name, out fullName))
            {
                var fs = this.AssetBundle.GetAllAssetNames();
                if (fs.Length == 1)
                {
                    fullName = fs[0];
                }
                else
                {
                    var subPath = ZString.Concat("/", name, ".");
                    fullName = fs.FirstOrDefault((p) => p.Contains(subPath, StringComparison.OrdinalIgnoreCase));
                }

                assetNameMap[name] = fullName;
            }

            if (fullName == null)
            {
                return null;
            }


            var go = this.AssetBundle.LoadAsset(fullName, type);
            return go;
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
            //卸载
            if (UseCounter <= 0)
            {
                this.AssetBundle.Unload(true);
            }
        }

        #endregion

        /// <summary>
        /// 卸载
        /// </summary>
        public void UnLoad()
        {
            this.UseCounter = 0;
            this.AssetBundle.Unload(true);
        }
    }
}
