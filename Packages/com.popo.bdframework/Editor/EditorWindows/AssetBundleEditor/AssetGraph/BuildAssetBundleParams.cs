using System;
using BDFramework.Core.Tools;
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
        /// 构建平台
        /// </summary>
        public BuildTarget BuildTarget;
        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputPath { get; set; } = BDApplication.DevOpsPublishAssetsPath;
    }
}