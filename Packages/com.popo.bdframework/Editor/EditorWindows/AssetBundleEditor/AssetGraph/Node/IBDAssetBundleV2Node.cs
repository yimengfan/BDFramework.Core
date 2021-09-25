using System.Collections.Generic;
using BDFramework.Editor.AssetBundle;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 序号
    /// </summary>
    public interface IBDAssetBundleV2Node
    {
        /// <summary>
        /// 当前buildinfo
        /// </summary>
        BuildInfo BuildInfo { get; }
    }
}