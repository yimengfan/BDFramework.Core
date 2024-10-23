using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.StringEx;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,不修改 只作为连线查看用 避免线到一坨了
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]按文件夹 Tag 打包", 30)]
    public class SetGranularityByFolderTag : SetGranularityBase
    {
        public AssetBundleBuildingContext BuildingCtx { get; set; }

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
            get { return "[颗粒度]按文件夹 Tag 打包"; }
        }


        /// <summary>
        /// 打包Tag
        /// </summary>
        public string Tag = "Null";

        /// <summary>
        /// 是否包含runtime资产
        /// </summary>
        public bool IsIncludeRuntimeAssets = false;

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            newData.AddDefaultInputPoint();
            newData.AddDefaultOutputPoint();
            return new SetGranularityByFolderTag();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            base.OnInspectorGUI(node, streamManager, inspector, onValueChanged);
            GUILayout.Space(10);

            bool isDirty = false;
            var label = EditorGUILayout.TextField("打包Tag", this.Tag);
            if (this.Tag != label)
            {
                isDirty = true;
                this.Tag = label;
            }

            //
            if (isDirty)
            {
                Debug.Log("更新node!");
                AssetGraphTools.UpdateNodeGraph(node);
            }
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

            //按tag文件夹进行打包
            if (!string.IsNullOrEmpty(this.Tag))
            {
                foreach (var ar in comingAssetReferenceList)
                {
                    //包含runtime资产的判断
                    if (AssetBundleToolsV2.IsRuntimePath(ar.importFrom) && !this.IsIncludeRuntimeAssets)
                    {
                        continue;
                    }

                    //
                    if (ar.importFrom.Contains(this.Tag, StringComparison.OrdinalIgnoreCase))
                    {
                        //
                        var staridx = ar.importFrom.IndexOf(this.Tag, StringComparison.OrdinalIgnoreCase);
                        string setABName = ar.importFrom.Substring(0, staridx + this.Tag.Length);

                        var (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(ar.importFrom, setABName, (BuildAssetInfos.SetABPackLevel) this.SetLevel, (this.selfNodeGUI != null ? this.selfNodeGUI.Name : this.GetHashCode().ToString()), false);
                        // if (!ret)
                        // {
                        //     Debug.LogError($"【颗粒度】设置AB失败 [{setABName}] - {ar.importFrom} \n {msg}");
                        // }
                    }
                }
            }
            
            
            
            //输出
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var ags in incoming)
            {
                foreach (var item in ags.assetGroups)
                {
                    outMap[item.Key] = new List<AssetReference>(item.Value);
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
