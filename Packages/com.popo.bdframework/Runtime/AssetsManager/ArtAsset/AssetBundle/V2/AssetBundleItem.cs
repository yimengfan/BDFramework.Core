using System.Collections.Generic;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// 存储单个资源的数据
    /// </summary>
    public class AssetBundleItem
    {
        public enum AssetTypeEnum
        {
            Others = 0,
            Prefab = 1,
            TextAsset = 2,
            Texture = 3,
            SpriteAtlas = 4,
            Mat,
            Shader,
            AudioClip,
            AnimationClip,
            Mesh,
            Font
        }

        public AssetBundleItem(int id, string loadPath, string assetbundlePath, AssetTypeEnum @enum, List<int> depend = null)
        {
            this.Id = id;
            this.LoadPath = loadPath;
            this.AssetBundlePath = assetbundlePath;
            this.Type = (int) @enum;
            this.DependAssetIds = depend;
        }

        /// <summary>
        /// 给各种序列化,用的构造
        /// </summary>
        public AssetBundleItem()
        {
        }

        /// <summary>
        /// 资源Id 
        /// </summary>
        public int Id { get; private set; }


        /// <summary>
        /// 映射，加载资源名。一般为程序调用加载的路径
        /// </summary>
        public string LoadPath { get; private set; }

        /// <summary>
        /// asset路径 【不序列化】
        /// </summary>
       // public string EditorAssetPath { get; set; }
        
        /// <summary>
        /// ab的资源路径名
        /// </summary>
        public string AssetBundlePath { get; private set; }

        /// <summary>
        /// 资源类型
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// 依赖
        /// </summary>
        public List<int> DependAssetIds { get; private set; } = new List<int>();
    }
}
