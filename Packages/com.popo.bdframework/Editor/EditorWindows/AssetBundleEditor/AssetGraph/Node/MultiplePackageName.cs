using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.VersionContrller;
using LitJson;
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

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }

            foreach (var ag in incoming)
            {
                foreach (var ags in ag.assetGroups)
                {
                    var ret = MultiplePackage.AssetMultiplePackageConfigList.FindIndex((mp) => mp.PackageName == this.PacakgeName);
                    AssetMultiplePackageConfigItem item = new AssetMultiplePackageConfigItem();
                    if (ret == -1)
                    {
                        item.AssetsDirectPathList = new List<string>();

                        MultiplePackage.AssetMultiplePackageConfigList.Add(item);
                    }
                    else
                    {
                        item = MultiplePackage.AssetMultiplePackageConfigList[ret];
                    }

                    //添加package的路径
                    item.AssetsDirectPathList.Add(ags.Key);
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
            var path = string.Format("{0}/{1}/{2}/{3}", BDFrameworkAssetsEnv.BuildParams.OutputPath, BDApplication.GetPlatformPath(buildTarget), BResources.ASSET_ROOT_PATH, BResources.SERVER_ASSETS_MULTIPLE_PACKAGE_CONFIG);
            FileHelper.WriteAllText(path, CsvSerializer.SerializeToString(MultiplePackage.AssetMultiplePackageConfigList));
            Debug.Log("保存分包设置:" + path);
        }
    }
}
