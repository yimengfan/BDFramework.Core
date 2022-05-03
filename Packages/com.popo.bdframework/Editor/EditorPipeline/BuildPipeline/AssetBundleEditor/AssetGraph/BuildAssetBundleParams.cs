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
        public BuildTarget BuildTarget = BuildTarget.NoTarget;
        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputPath { get; set; } = BApplication.DevOpsPublishAssetsPath;

        /// <summary>
        /// 是否正在打包
        /// </summary>
        public bool IsBuilding { get; set; } = false;
    }
}