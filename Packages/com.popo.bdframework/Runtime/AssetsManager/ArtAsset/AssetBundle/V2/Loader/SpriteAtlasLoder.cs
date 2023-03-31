using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// 图集加载器
    /// </summary>
    public class SpriteAtlasLoder: AssetLoder
    {
        public SpriteAtlasLoder(AssetBundle ab) : base(ab)
        {
        }
        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        public Object LoadSpriteFormAtlas(string texName)
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
        
        
        /// <summary>
        /// 加载图集资源,仅支持 unity atlas方案
        /// </summary>
        /// <param name="texName"></param>
        /// <returns></returns>
        public SpriteAtlas LoadAtlas()
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





    }
}
