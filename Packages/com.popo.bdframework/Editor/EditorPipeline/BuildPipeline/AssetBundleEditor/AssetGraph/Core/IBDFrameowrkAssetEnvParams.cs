using System.Collections.Generic;
using BDFramework.Editor.AssetBundle;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 序号
    /// </summary>
    public interface IBDFrameowrkAssetEnvParams
    {
        /// <summary>
        /// 当前buildinfo
        /// </summary>
        BuildAssetsInfo BuildAssetsInfo { get; set; }
        /// <summary>
        /// Build参数
        /// </summary>
        BuildAssetBundleParams BuildParams { get; set; }

        /// <summary>
        /// 重置
        /// </summary>
        void Reset();
    }
}