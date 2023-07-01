using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.ResourceMgr.V2;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
using UnityEngine.Video;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 因为unity有bug video不支持lz压缩格式
    /// </summary>
    [CustomNode("BDFramework/[逻辑]搜集Video", 60)]
    public class CollectVideoClip : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 构建的上下文信息
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }

        public void Reset()
        {
        }

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

        private NodeGUI selfNodeGUI;
        public bool IsOnlyOneShaderVaraint = false;

        public override void OnDrawNodeGUIContent(NodeGUI node)
        {
            base.OnDrawNodeGUIContent(node);
            this.selfNodeGUI = node;
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
            if (BuildingCtx.BuildParams.IsBuilding)
            {
                EditorUtility.DisplayProgressBar("构建资产", this.Category, 1);
            }

            AssetGraphTools.WatchBegin();


            var videoClipList = new List<AssetReference>();

            //输出传入的
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var assetgroup in incoming)
            {
                foreach (var group in assetgroup.assetGroups)
                {
                    //不直接操作传入的容器存储
                    var newAssetList = group.Value.ToList();

                    foreach (var ar in group.Value)
                    {
                        if (ar.assetType == typeof(VideoClip))
                        {
                            videoClipList.Add(ar);
                            newAssetList.Remove(ar);
                        }
                    }


                    outMap[group.Key] = newAssetList;
                }
            }

            AssetGraphTools.WatchEnd("【搜集图集】");
            //atlas
            //outMap[nameof(BDFrameworkAssetsEnv.FloderType.VideoClip)] = videoClipList.ToList();
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }

        /// <summary>
        /// 设置VideoClip相关设置
        /// </summary>
        public void SetAllSpriteAtlasAB(List<AssetReference> atlasAssetReferenceList)
        {
            for (int i = 0; i < atlasAssetReferenceList.Count; i++)
            {
                var vedioAR = atlasAssetReferenceList[i];
                //获取依赖中的tex,并设置AB名为atlas名
                var assetInfo = this.BuildingCtx.BuildAssetInfos.GetAssetInfo(vedioAR.importFrom);
                if (assetInfo != null)
                {
                    var log = this.Category + " " + (this.selfNodeGUI != null ? this.selfNodeGUI.Name : this.GetHashCode().ToString());
                    var (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(vedioAR.importFrom, vedioAR.importFrom, BuildAssetInfos.SetABPackLevel.Lock, log, false);
                }

                //设置ABLoadType
                this.BuildingCtx.BuildAssetInfos.SetABLoadType(vedioAR.importFrom, AssetLoaderFactory.AssetBunldeLoadType.Base);
            }
        }
    }
}