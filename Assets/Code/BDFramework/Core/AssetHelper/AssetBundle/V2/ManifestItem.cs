using System.Collections.Generic;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// 存储单个资源的数据
    /// </summary>
    public class ManifestItem
    {
        public enum AssetTypeEnum
        {
            Others = 0,
            Prefab = 1,
            TextAsset = 2,
            Texture = 3,
            SpriteAtlas = 4,
        }

        public ManifestItem(string path, string ab, AssetTypeEnum @enum, List<string> depend = null)
        {
            this.Path = path;
            this.AB = ab;
            this.Type = (int) @enum;
            this.Depend = depend;
        }


        /// <summary>
        /// 资源名,单ab 单资源情况下. name = ab名
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// 单ab 多资源情况下，packagename 就是ab名 
        /// </summary>
        public string AB { get; private set; }

        /// <summary>
        /// asset类型
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// 依赖
        /// </summary>
        public List<string> Depend { get; set; } = new List<string>();
    }
}