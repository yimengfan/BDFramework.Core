using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public AssetBundle AssetBundle;

        #region 各种加载接口

        Dictionary<string, string> assetNameMap = new Dictionary<string, string>();

        /// <summary>
        /// 加载图集资源
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        public Object LoadTextureFormAtlas(string texName)
        {
            //默认一个ab中只有一个atlas
            var fs    = AssetBundle.GetAllAssetNames();
            var atlas = this.AssetBundle.LoadAsset<SpriteAtlas>(fs[fs.Length - 1]);
            texName = Path.GetFileName(texName);
            var sp = atlas.GetSprite(texName);
            return sp;
        }

        /// <summary>
        /// 加载普通资源
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object LoadAsset(string name, Type type)
        {
            string realname = "";
            if (!assetNameMap.TryGetValue(name, out realname))
            {
                var fs = this.AssetBundle.GetAllAssetNames().ToList();
                if (fs.Count == 1)
                {
                    realname = fs[0];
                }
                else
                {
                    var _name = name.ToLower() + ".";
                    realname = fs.Find((p) => p.Contains(_name));
                }

                assetNameMap[name] = realname;
            }

            if (realname == null)
                return null;
            return this.AssetBundle.LoadAsset(realname, type);
        }

        #endregion

        #region 引用计数

        public int Counter { get; private set; }

        public void Use()
        {
            Counter++;
        }

        public void Unuse()
        {
            Counter--;
            if (Counter <= 0)
            {
                if (AssetBundle)
                {
                    AssetBundle.Unload(true);
                }
            }
        }

        #endregion
    }
}