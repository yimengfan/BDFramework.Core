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
    /// 颗粒度,排序30-50
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]按文件夹打包", 30)]
    public class SetGranularityByFolder : SetGranularityBase
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
            get { return "[颗粒度]整个文件夹打包"; }
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
            return new SetGranularityByFolder();
        }
        
        //
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
            EditorGUILayout.HelpBox("将该目录下所有文件,打包成一个AB!", MessageType.Info);
            base.OnInspectorGUI(node, streamManager, inspector, onValueChanged);
        }

        /// <summary>
        /// 预览结果 编辑器连线数据，但是build模式也会执行
        /// 这里注意不要对BuildingCtx直接进行修改,修改需要在Build中进行
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
            
            
            //色湖之颗粒度

            SetABNameUseThisFolderName(incoming);

            //按buildinfo中的依赖关系输出
            var outMap = new Dictionary<string, List<AssetReference>>();
            var incomingList = AssetGraphTools.GetComingAssets(incoming);
            foreach (var ar in incomingList)
            {
                var ai = this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                if (ai != null)
                {
                    if (!outMap.ContainsKey(ai.ABName))
                    {
                        outMap[ai.ABName] = new List<AssetReference>();
                    }
                    outMap[ai.ABName].Add(ar);
                }
                else
                {
                    Debug.LogError("不存在资产:" + ar.importFrom);
                }
            }
            
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }


        /// <summary>
        /// 用该目录设置AB name
        /// </summary>
        private void SetABNameUseThisFolderName(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    var folderPath = ag.Key;

                    foreach (var ar in ag.Value)
                    {
                        //设置当前ab名为文件夹名
                        var (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(ar.importFrom, folderPath, (BuildAssetInfos.SetABPackLevel) this.SetLevel, (this.selfNodeGUI!=null?this.selfNodeGUI.Name: this.GetHashCode().ToString()), false);
                        // if (!ret)
                        // {
                        //     Debug.LogError($"【颗粒度】设置AB失败 [{folderPath}] - {ar.importFrom} \n {msg}");
                        // }

                        //设置依赖，依赖资产需要特殊处理在当前根目录下的依赖，跳过按文件夹处理
                        if (this.IsIncludeDependAssets)
                        {
                          var ai=    this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                            if (ai != null)
                            {
                                foreach (var depend in ai.DependAssetList)
                                {
                                    //在不在同级根目录判断
                                    if (!depend.Equals(ar.importFrom, StringComparison.OrdinalIgnoreCase) && !depend.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(depend, folderPath, (BuildAssetInfos.SetABPackLevel) this.SetLevel, this.Category + " " + (this.selfNodeGUI!=null?this.selfNodeGUI.Name: this.GetHashCode().ToString()), false);
                                        // if (!ret)
                                        // {
                                        //     Debug.LogError($"【颗粒度】[depend]设置AB失败 [{folderPath}] - {ar.importFrom} \n {msg}");
                                        // }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
