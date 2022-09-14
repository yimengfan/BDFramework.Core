using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 保留GUID
    /// </summary>
    [CustomNode("BDFramework/[设置]使用GUID加载", 70)]
    public class SetKeepGUID : UnityEngine.AssetGraph.Node
    {
        public AssetBundleBuildingContext BuildingCtx { get; set; }
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
            get { return "[设置]使用GUID加载"; }
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
            return new SetGranularityNull();
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

            Debug.Log("-->保留GUID加载！");
            if (incoming == null)
            {
                return;
            }
            //搜集所有的 asset reference 
            var comingAssetReferenceList = AssetGraphTools.GetComingAssets(incoming);
            if (comingAssetReferenceList.Count == 0)
            {
                return;
            }
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;
            
            var outMap = new Dictionary<string, List<AssetReference>>();
            
            foreach (var ags in incoming)
            {
                foreach (var assetGroup in ags.assetGroups)
                {
                    outMap[assetGroup.Key] = assetGroup.Value.ToList();

                    //设置这些资产 保留GUID
                    foreach (var ar in assetGroup.Value)
                    {
                        if (BuildingCtx != null)
                        {
                            var ai =   this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                            if (ai != null)
                            {
                                ai.IsKeepGUID = true;
                            }
                        }
                       
                    }
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