using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[Loder]加载Runtime资产路径", 1)]
    public class LoderBuiltinRuntimeAssetDirectories : UnityEngine.AssetGraph.Node
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
            get { return "[Loder]加载Runtime资产路径"; }
        }

        public override void Initialize(NodeData data)
        {
           
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new LoderBuiltinRuntimeAssetDirectories();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
        }

        /// <summary>
        /// 预览结果 编辑器连线数据，但是build模式也会执行
        /// 这里只建议设置BuildingCtx的ab颗粒度
        /// </summary>
        /// <param name="target"></param>
        /// <param name="nodeData"></param>
        /// <param name="incoming"></param>
        /// <param name="connectionsToOutput"></param>
        /// <param name="outputFunc"></param>
        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            
            //输出传入的
            var outMap = new Dictionary<string, List<AssetReference>>();
            var runtimeDirs =  BApplication.GetAllRuntimeDirects();
            foreach (var dir in runtimeDirs)
            {
                //这里拿key当参数传递
                outMap[dir] = new List<AssetReference>();
            }
            
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }


    }
}
