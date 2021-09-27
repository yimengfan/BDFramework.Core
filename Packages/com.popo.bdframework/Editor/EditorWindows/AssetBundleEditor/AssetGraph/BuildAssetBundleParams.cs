using System;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 构建Assetbundle的参数
    /// </summary>
    [Serializable]
    public class BuildAssetBundleParams
    {
        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputPath;
        /// <summary>
        /// 构建平台
        /// </summary>
        public RuntimePlatform Platform;
        /// <summary>
        /// 构建ab参数
        /// </summary>
        public BuildAssetBundleOptions Options = BuildAssetBundleOptions.ChunkBasedCompression;
        /// <summary>
        /// 是否使用hash
        /// </summary>
        public bool IsHashName = false;
    }
}