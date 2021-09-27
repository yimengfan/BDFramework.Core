using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/搜集shader变体", 50)]
    public class CollectShaderKeyWord : UnityEngine.AssetGraph.Node, IBDAssetBundleV2Node
    {
        public BuildInfo BuildInfo { get; private set; }

        public override string ActiveStyle
        {
            get { return "node 7 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 7"; }
        }

        public override string Category
        {
            get { return "搜集Shader变体"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new CollectShaderKeyWord();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
        }

        //后缀和ab名
        private List<string> FileExtens = new List<string>() {".shader", ".shadervariants"};
        private string AssetBundleName = ShaderCollection.ALL_SHADER_VARAINT_PATH;

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }

            this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;

            //开始搜集shader varint
            var outMap = new Dictionary<string, List<AssetReference>>();
            var shaderAndVariantList = new List<AssetReference>();
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    var newList = group.Value.ToList();
                    for (int i = newList.Count - 1; i >= 0; i--)
                    {
                        //不直接操作传入的容器存储
                        var af = newList[i];
                        if (FileExtens.Contains(af.extension))
                        {
                            newList.RemoveAt(i);
                            shaderAndVariantList.Add(af);
                        }
                    }

                    //输出
                    outMap[group.Key] = newList;
                }
            }

            //设置ab
            foreach (var sharder in shaderAndVariantList)
            {
                this.BuildInfo.SetABName(sharder.importFrom, AssetBundleName, BuildInfo.SetABNameMode.Force);
            }

            //输出shader
            outMap[nameof(BDFrameworkAssetsEnv.FloderType.Shaders)] = shaderAndVariantList;
            //输出
            outputFunc(connectionsToOutput.FirstOrDefault(), outMap);
        }
    }
}
