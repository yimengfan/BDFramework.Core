using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.BuildPipeline.AssetBundle
{
    /// <summary>
    /// AssetGrah的辅助工具
    /// </summary>
    public class AssetGraphTools
    {
        static private Stopwatch sw = new Stopwatch();

        /// <summary>
        /// 监测开始
        /// </summary>
        static public void WatchBegin()
        {
            sw.Restart();
        }
        
        /// <summary>
        /// 检测结束
        /// </summary>
        /// <param name="title"></param>
        static public void WatchEnd(string title = "")
        {
            sw.Stop();

            UnityEngine.Debug.LogFormat("{0}耗时:{1}ms", title, sw.ElapsedMilliseconds);
        }


        /// <summary>
        /// 输入的资产整理
        /// </summary>
        /// <returns></returns>
        static public List<AssetReference> GetComingAssets(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            var comingAssetReferenceList = new List<AssetReference>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    comingAssetReferenceList.AddRange(ag.Value);
                }
            }
            return comingAssetReferenceList;
        }
        
        

        
        
        
        #region 节点刷新逻辑

        /// <summary>
        /// 刷新节点值
        /// </summary>
        /// <param name="nodeGUI"></param>
        static public void UpdateNodeGraph(NodeGUI nodeGUI)
        {
            //强制刷新
            nodeGUI.Data.NeedsRevisit = true;
            nodeGUI.Data.Operation.Save(); 
            nodeGUI.ParentGraph.SetGraphDirty(); 
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_UPDATED, nodeGUI));
        }

        /// <summary>
        /// 更新连接线
        /// </summary>
        /// <param name="nodeGUI"></param>
        /// <param name="outputConnect"></param>
        static public void UpdateConnectLine(NodeGUI nodeGUI, ConnectionPointData outputConnect)
        {
            if (outputConnect == null)
            {
                return;
            }
            nodeGUI.Data.NeedsRevisit = true;
            nodeGUI.Data.Operation.Save(); 
            nodeGUI.ParentGraph.SetGraphDirty(); 
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, nodeGUI, Vector2.zero, outputConnect));
        }

        /// <summary>
        /// 移除输出节点
        /// </summary>
        /// <param name="nodeGUI"></param>
        /// <param name="outputConnect"></param>
        static public void RemoveOutputNode(NodeGUI nodeGUI, ConnectionPointData outputConnect)
        {
            if (outputConnect == null)
            {
                return;
            }
            nodeGUI.Data.OutputPoints.Remove(outputConnect);
            nodeGUI.Data.NeedsRevisit = true;
            nodeGUI.Data.Operation.Save(); 
            nodeGUI.ParentGraph.SetGraphDirty(); 
            //移除连接线
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, nodeGUI, Vector2.zero, outputConnect));
        }

        #endregion

    }
}
