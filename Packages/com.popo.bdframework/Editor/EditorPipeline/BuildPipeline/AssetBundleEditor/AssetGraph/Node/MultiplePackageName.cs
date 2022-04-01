using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.VersionController;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,不修改 只作为连线查看用 避免线到一坨了
    /// </summary>
    [CustomNode("BDFramework/[分包]设置分包名", 21)]
    public class MultiplePackageName : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 构建的上下文信息
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }
        public override string ActiveStyle
        {
            get { return "node 3 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 3"; }
        }

        public override string Category
        {
            get { return "[分包]设置分包名"; }
        }


        public string PacakgeName = "";

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();

            return new MultiplePackageName();
        }


        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            this.PacakgeName = EditorGUILayout.TextField("分包名:", this.PacakgeName);
            node.Name = "分包名:" + this.PacakgeName;
            //editor.UpdateNodeName(node);
        }

        /// <summary>
        /// 包目录列表
        /// </summary>
        private List<string> packageAssetList = new List<string>();

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;

            //
            packageAssetList = new List<string>();
            foreach (var ag in incoming)
            {
                foreach (var ags in ag.assetGroups)
                {
                    foreach (var ar in ags.Value)
                    {
                        packageAssetList.Add(ar.importFrom);
                    }
                }
            }
        }

        /// <summary>
        /// 保存分包设置
        /// </summary>
        /// <param name="ctx"></param>
        public override void Build(BuildTarget buildTarget, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc,
            Action<NodeData, string, float> progressFunc)
        {
            var assetIdList = new List<int>();
            //寻找当前分包,包含的资源
            foreach (var asset in this.packageAssetList)
            {
                BuildAssetBundle.BuildAssetsResult.AssetDataMaps.TryGetValue(asset, out var buildAssetData);

                //依次把加入资源和依赖资源
                foreach (var dependHash in buildAssetData.DependAssetList)
                {
                    var dependAsset = BuildAssetBundle.BuildAssetsResult.AssetDataMaps.Values.FirstOrDefault((value) => value.ArtConfigIdx != -1 && value.ABName == dependHash);
                    if (dependAsset != null)
                    {
                        assetIdList.Add(dependAsset.ArtConfigIdx);
                    }
                    else
                    {
                        BDebug.LogError("分包依赖失败:" + dependHash);
                    }
                }

                //符合分包路径
                assetIdList.Add(buildAssetData.ArtConfigIdx);
            }

            //新建package描述表
            var subPackage = new SubPackageConfigItem();
            subPackage.PackageName = this.PacakgeName;
            //热更资源
            subPackage.ArtAssetsIdList = assetIdList.Distinct().ToList();
            subPackage.ArtAssetsIdList.Sort();
            //热更代码
            subPackage.HotfixCodePathList.Add(ScriptLoder.DLL_PATH);
            //热更表格
            subPackage.TablePathList.Add(SqliteLoder.LOCAL_DB_PATH);
            //配置表
            subPackage.ConfAndInfoList.Add(BResources.ASSET_CONFIG_PATH);
            subPackage.ConfAndInfoList.Add(BResources.ASSET_TYPES_PATH);
            subPackage.ConfAndInfoList.Add(BResources.PACKAGE_BUILD_INFO_PATH);

            MultiplePackage.AssetMultiplePackageConfigList.Add(subPackage);
            //
            var path = string.Format("{0}/{1}/{2}", this.BuildingCtx.BuildParams.OutputPath, BDApplication.GetPlatformPath(buildTarget), BResources.SERVER_ASSETS_SUB_PACKAGE_CONFIG_PATH);
            var csv = CsvSerializer.SerializeToString(MultiplePackage.AssetMultiplePackageConfigList);
            FileHelper.WriteAllText(path, csv);
            Debug.Log("保存分包设置:" + this.PacakgeName + " -" + buildTarget.ToString());
        }
    }
}
