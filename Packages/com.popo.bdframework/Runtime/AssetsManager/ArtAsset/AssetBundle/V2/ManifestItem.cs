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
            Mat,
            Shader,
            AudioClip,
            AnimationClip,
            Mesh,
            Font
            
        }

        public ManifestItem(string path, AssetTypeEnum @enum, List<string> depend = null)
        {
            this.Path = path;
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
        /// ab的资源路径名
        /// </summary>
        public string Path { get; private set; }
        
        /// <summary>
        /// 资源类型
        /// </summary>
        public int Type { get;  private  set; }

        /// <summary>
        /// 依赖
        /// </summary>
        public List<string> Depend { get;  private set; } = new List<string>();
    }
}