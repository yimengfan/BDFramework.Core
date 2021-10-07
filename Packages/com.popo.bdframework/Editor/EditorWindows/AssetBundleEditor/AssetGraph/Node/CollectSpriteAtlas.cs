using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[逻辑]搜集图集", 60)]
    public class CollectSpriteAtlas : UnityEngine.AssetGraph.Node, IBDFrameowrkAssetEnvParams
    {
        public BuildInfo BuildInfo { get; set; }
        public BuildAssetBundleParams BuildParams { get; set; }

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
            get { return "搜集图集"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new CollectSpriteAtlas();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
        }

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }
            if (this.BuildInfo == null)
            {
                this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;
            }
            if (this.BuildParams == null)
            {
                this.BuildParams = BDFrameworkAssetsEnv.BuildParams;
            }
            //找到runtime
            List<AssetReference> runtimeAssetReferenceList = null;
            incoming.FirstOrDefault()?.assetGroups.TryGetValue(nameof(BDFrameworkAssetsEnv.FloderType.Runtime), out runtimeAssetReferenceList);
            //获取所有的图集设置
            var atlasAssetReferenceList = runtimeAssetReferenceList.FindAll((af) => af.extension == ".spriteatlas");
            this.SetAllSpriteAtlasAB(atlasAssetReferenceList);
            //输出传入的
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var assetgroup in incoming)
            {
                foreach (var group in assetgroup.assetGroups)
                {
                    if (group.Key == nameof(BDFrameworkAssetsEnv.FloderType.Runtime)) //runtime 特殊处理
                    {
                        //不直接操作传入的容器存储
                        var newAssetList = group.Value.ToList();
                        foreach (var atlas in atlasAssetReferenceList)
                        {
                            newAssetList.Remove(atlas);
                        }

                        outMap[group.Key] = newAssetList;
                    }
                    else
                    {
                        outMap[group.Key] = group.Value.ToList();
                    }
                }
            }

            //atlas
            outMap[nameof(BDFrameworkAssetsEnv.FloderType.SpriteAtlas)] = atlasAssetReferenceList.ToList();
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }

        /// <summary>
        /// 设置图集相关的AB
        /// </summary>
        public void SetAllSpriteAtlasAB(List<AssetReference> atlasAssetReferenceList)
        {
            for (int i = 0; i < atlasAssetReferenceList.Count; i++)
            {
                var atlasAR = atlasAssetReferenceList[i];
                //获取依赖中的tex,并设置AB名为atlas名
                if (this.BuildInfo.AssetDataMaps.TryGetValue(atlasAR.importFrom, out BuildInfo.BuildAssetData atlasAssetData))
                {
                    //设置tex ab
                    foreach (var dependTex in atlasAssetData.DependAssetList)
                    {
                        var ret = this.BuildInfo.SetABName(dependTex, atlasAR.importFrom, BuildInfo.SetABNameMode.Force);
                    }
                }
            }
        }
    }
}
