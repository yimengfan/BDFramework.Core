using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 演示地图资源的颗粒度拓展
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]地图资源-拓展", 30)]
    public class SetGranularity_Map : UnityEngine.AssetGraph.Node
    {
        public override string ActiveStyle
        {
            get { return "node 4 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 4"; }
        }

        public override string Category
        {
            get { return "[颗粒度]地图资源-拓展demo"; }
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
            return new SetGranularity_Map();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
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
            //没有输入连线时候判断
            if (incoming == null)
            {
                return;
            }
            var outMap = new Dictionary<string, List<AssetReference>>();
            //1.遍历传入的节点连线，每个item则为一根传入的线
            foreach (var ags in incoming)
            {
                //2.遍历每根线的输入内容
                foreach (var assetGroup in ags.assetGroups)
                {
                    //这里注意为了防止 下个节点逻辑对传出数据影响，所以这里tolist进行copy一次
                    outMap[assetGroup.Key] = assetGroup.Value.ToList();
                }
            }


            //只对第一个节点输出
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                //3.输出的一根线内容（其实也是下个节点遍历内容）
                outputFunc(output, outMap);
            }
        }
    }
}