using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,排序30-50
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]文件夹规则", 30)]
    public class SetGranularityByFloder : UnityEngine.AssetGraph.Node, IBDAssetBundleV2Node
    {
        public BuildInfo BuildInfo { get; private set; }

        /// <summary>
        /// 文件夹AB规则
        /// </summary>
        public enum FloderAssetBundleRule
        {
            /// <summary>
            /// 设置AB名为父文件夹路径
            /// </summary>
            SetABAsFloderPath,

            /// <summary>
            /// 设置所有子文件夹中的文件，AB名为子文件夹名
            /// </summary>
            SetAllChildFloderPath
        }
        
        /// <summary>
        /// 设置规则
        /// </summary>
        private FloderAssetBundleRule BuildRule = FloderAssetBundleRule.SetABAsFloderPath;
        public FloderAssetBundleRule SetWarpper_BuildRule
        {
            set
            {
                if (value != BuildRule)
                {
                    BuildRule = value;
                    onInspectorValueChanged?.Invoke();
                }
            }
        }

        public override string ActiveStyle
        {
            get { return "node 6 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 6"; }
        }

        public override string Category
        {
            get { return "[颗粒度]文件夹规则"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            return new SetGranularityByFloder();
        }

        private Action onInspectorValueChanged;
        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
            NodeGUIEditor editor,
            Action onValueChanged)
        {
            onInspectorValueChanged = onValueChanged;
            this.SetWarpper_BuildRule = (FloderAssetBundleRule) EditorGUILayout.EnumPopup("设置规则", this.BuildRule);
        }

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            Debug.Log("刷新值!" +  DateTime.Now.ToLongTimeString());
            if (incoming == null)
            {
                return;
            }

            this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;

            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    var floderPath = ag.Key;

                    foreach (var ar in ag.Value)
                    {
                        //设置当前ab名为文件夹名,不覆盖在此之前的规则
                        var ret = BuildInfo.SetABName(ar.importFrom, floderPath);
                        if (!ret)
                        {
                            Debug.LogError($"【颗粒度】设置AB失败 [{floderPath}] -" + ar.importFrom);
                        }
                    }

                    outMap[ag.Key] = ag.Value.ToList();
                }
            }

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }
    }
}
