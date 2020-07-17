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

        public ManifestItem(string path, AssetTypeEnum @enum, List<string> depend = null)
        {
            this.Path = path;
            //this.AB = ab;
            this.Type = (int) @enum;
            this.Depend = depend;
        }

        /// <summary>
        /// 给litjson 用的构造
        /// </summary>
        public ManifestItem()
        {
            
        }

        /// <summary>
        /// 资源名,单ab 单资源情况下. name = ab名
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///  AB为空则用AB加载,用默认情况下以Path为AB名,
        /// </summary>
        //public string AB { get;  private  set; }

        /// <summary>
        /// asset类型
        /// </summary>
        public int Type { get;  private  set; }

        /// <summary>
        /// 依赖
        /// </summary>
        public List<string> Depend { get;  private set; } = new List<string>();
    }
}