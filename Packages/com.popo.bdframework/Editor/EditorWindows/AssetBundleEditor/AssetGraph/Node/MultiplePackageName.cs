using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using LitJson;
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
                    var ret = MultiplePackage.PackageNameMap.TryGetValue(this.PacakgeName, out var list);
                    if (!ret)
                    {
                        list = new List<string>();
                        MultiplePackage.PackageNameMap[this.PacakgeName] = list;
                    }

                    //添加package的路径
                    list.Add(ags.Key);
                }
            }
        }

        public override void Build(NodeBuildContext ctx)
        {
            var path = BDFrameworkAssetsEnv.BuildParams.OutputPath + "/" + BDApplication.GetPlatformPath(ctx.target) + "/" + BResources.ASSET_PACKAGE_CONFIG_PATH;
            FileHelper.WriteAllText(path, JsonMapper.ToJson(MultiplePackage.PackageNameMap));
            Debug.Log("保存分包:" + path);
        }
    }
}
