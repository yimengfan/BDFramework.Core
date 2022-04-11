using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 节点辅助类
    /// </summary>
    static public class GraphNodeHelper
    {
        /// <summary>
        /// 获取所有的输入资源
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        static public List<AssetReference> GetAllComingAssets(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            var retList = new List<AssetReference>();

            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    retList.AddRange(ag.Value);
                }
                
            }
            return retList;
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
