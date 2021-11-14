using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Core.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 加载目录资源
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]加载BResources资源", 10)]
    public class Loder : UnityEngine.AssetGraph.Node
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
            get { return "[颗粒度]加载BResources Runtime目录"; }
        }


        public override void Initialize(NodeData data)
        {
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            return new SetGranularityNull();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
        }


        /// <summary>
        /// 缓存
        /// </summary>
        private static Dictionary<string, AssetReference> AssetReferenceCacheMap = new Dictionary<string, AssetReference>();

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            var outMap = new Dictionary<string, List<AssetReference>>();
            var allRuntimeDirects = BDApplication.GetAllRuntimeDirects();
            
            foreach (var runtimePath in allRuntimeDirects)
            {
                //创建
                outMap[runtimePath] = new List<AssetReference>();
                var loadAssetGuids = AssetDatabase.FindAssets("",new string[]{runtimePath});
                //处理输出
                foreach (var guid in loadAssetGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var ret = AssetReferenceCacheMap.TryGetValue(path, out var outAR);
                    if (!ret)
                    {
                        var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                        if (type == null)
                        {
                            Debug.LogError("【Loder】无法获取资源类型:" + path);
                            continue;
                        }

                        outAR = AssetReference.CreateReference(path);
                        AssetReferenceCacheMap[path] = outAR;
                    }
                    //添加输出
                    outMap[runtimePath].Add(outAR);
                }
            }

            //
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }
    }
}
