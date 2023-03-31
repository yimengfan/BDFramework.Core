using UnityEngine;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// AssetLoder加载工厂
    /// </summary>
    static public class AssetLoaderFactory
    {
        /// <summary>
        /// 有些特殊的资产，会打成一个ab
        /// </summary>
        public enum AssetBunldeLoadType
        {
            Base = 0,
            SpriteAtlas = 1,
            ShaderVaraint = 2,
        }


        /// <summary>
        /// 根据不同情况创建loader
        /// </summary>
        /// <param name="item"></param>
        /// <param name="ab"></param>
        /// <returns></returns>
        static public AssetLoder CrateAssetLoder(AssetBundleItem item, AssetBundle ab)
        {
            switch (item.AssetBundleLoadType)
            {
                case (int)AssetBunldeLoadType.SpriteAtlas:
                    return new SpriteAtlasLoder(ab);

                case (int)AssetBunldeLoadType.ShaderVaraint:
                    return new ShaderLoder(ab);

                default:
                    return new AssetLoder(ab);
            }
        }
    }
}
