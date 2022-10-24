using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度按Prefab打包，
    /// 允许传入Prefab和被prefab的资源，其他的则会报错
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]按Prefab打包", 32)]
    public class SetGranularityByPrefab : SetGranularityBase
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
            get { return "[颗粒度]单Prefab打包"; }
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
            return new SetGranularityByPrefab();
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
        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {

            EditorGUILayout.HelpBox("将该目录下所有Prefab打包!", MessageType.Info);

            base.OnInspectorGUI( node,  streamManager,  inspector,  onValueChanged);
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

            var outMap = new Dictionary<string, List<AssetReference>>(StringComparer.OrdinalIgnoreCase);
            var prefabExt = ".prefab";
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    foreach (var ar in ag.Value)
                    {
                        if (!ar.extension.Equals(prefabExt, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var (ret,msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(ar.importFrom, ar.importFrom, BuildAssetInfos.SetABPackLevel.Simple, this.Category + " " + (this.selfNodeGUI!=null?this.selfNodeGUI.Name: this.GetHashCode().ToString()), this.IsIncludeDependAssets);
                        // if (!ret)
                        // {
                        //     Debug.LogError($"【颗粒度】设置AB失败 - {ar.importFrom} \n {msg}");
                        // }

                        //返回prefab
                        outMap[ar.importFrom] = new List<AssetReference>() { };
                    }
                }
            }

            //按ab颗粒度输出
            outMap["error"] = new List<AssetReference>();
            var incomingList = AssetGraphTools.GetComingAssets(incoming);
            foreach (var ar in incomingList)
            {
               var ai =   this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                if (ai != null)
                {
                    if (!outMap.ContainsKey(ai.ABName))
                    {
                        Debug.LogWarning($"传入资产错误：ab依赖不在当前传入列表中!\n asset: {ar.importFrom} - ab:{ai.ABName} \n {JsonMapper.ToJson(ai, true)}");
                        outMap["error"].Add(ar);
                    }
                    else
                    {
                        //加入到输出列表
                        outMap[ai.ABName].Add(ar);
                    }
                }
                else
                {
                    Debug.LogError($"{this.Category}-资产不存在{ar.importFrom}");
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
