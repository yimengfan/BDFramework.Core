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
        public string OutputPath = Application.streamingAssetsPath;
        /// <summary>
        /// 是否使用hash
        /// </summary>
        public bool IsUseHashName = false;
    }
}