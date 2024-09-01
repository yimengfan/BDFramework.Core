using System.Collections.Generic;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// 资源类型列表
    /// </summary>
    // public class AssetType
    // {
    //     /// <summary>
    //     /// Prefab
    //     /// </summary>
    //     static public int VALID_TYPE_PREFAB = -1;
    //
    //     /// <summary>
    //     /// sprite
    //     /// </summary>
    //     static public int VALID_TYPE_SPRITE = -1;
    //     
    //     /// <summary>
    //     /// 图集
    //     /// </summary>
    //     static public int VALID_TYPE_SPRITE_ATLAS = -1;
    // }

    /// <summary>
    /// 资源类型列表
    /// </summary>
    public class AssetTypeConfig
    {
        public List<string> AssetTypeList { get; set; }
    }
}
