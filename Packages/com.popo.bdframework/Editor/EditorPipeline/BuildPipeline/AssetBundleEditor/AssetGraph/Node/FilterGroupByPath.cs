using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 筛选,排序10-30
    /// </summary>
    [CustomNode("BDFramework/[筛选]Group by Path", 10)]
    public class FilterGroupByPath : UnityEngine.AssetGraph.Node
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
            get { return "node 2 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 2"; }
        }

        public override string Category
        {
            get { return "[筛选]Group by Path"; }
        }


        /// <summary>
        /// 输出路径的数据
        /// </summary>
        [Serializable]
        public class GroupPathData
        {
            public string OutputNodeId;
            public string GroupPath;
        }

        /// <summary>
        /// 所有输出路径
        /// 这里的值一定要public，不然sg 用json序列化判断值未变化，则不会刷新
        /// </summary>
        [SerializeField]
        public List<GroupPathData> groupFilterPathDataList = new List<GroupPathData>();

        /// <summary>
        /// 路径list渲染对象
        /// </summary>
        ReorderableList e_groupList;


        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            var node = new FilterGroupByPath();
            newData.AddDefaultInputPoint();
            return node;
        }

        #region 渲染 list Inspector

        private NodeGUI selfNodeGUI;

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            this.selfNodeGUI = node;
            //初始化group list
            if (e_groupList == null)
            {
                e_groupList                     = new ReorderableList(groupFilterPathDataList, typeof(string), false, false, true, true);
                e_groupList.onReorderCallback   = ReorderFilterEntryList;
                e_groupList.onAddCallback       = AddToFilterEntryList;
                e_groupList.onRemoveCallback    = RemoveFromFilterEntryList;
                e_groupList.drawElementCallback = DrawFilterEntryListElement;
                e_groupList.onCanRemoveCallback = (list) =>
                {
                    if (e_groupList.count <= 2)
                    {
                        return false;
                    }

                    return true;
                };
                e_groupList.onChangedCallback = OnChangeList;
                e_groupList.elementHeight     = EditorGUIUtility.singleLineHeight + 8f;
                e_groupList.headerHeight      = 3;
                e_groupList.index             = this.groupFilterPathDataList.Count - 1;
            }

            //添加输出节点
            if (incommingAssetGroup != null)
            {
                foreach (var ag in incommingAssetGroup.assetGroups)
                {
                    this.AddOutputNode(ag.Key);
                }
            }


            GUILayout.Label("路径匹配:建议以\"/\"结尾,不然路径中包含这一段path都会被匹配上.");
            e_groupList.DoLayoutList();
        }


        private void RemoveFromFilterEntryList(ReorderableList list)
        {
            if (list.count > 2)
            {
                //移除序列化值
                var removeIdx = this.groupFilterPathDataList.Count - 1;
                var rItem     = this.groupFilterPathDataList[removeIdx];
                this.groupFilterPathDataList.RemoveAt(removeIdx);
                //移除输出节点
                var rOutputNode = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == rItem.OutputNodeId);
                this.selfNodeGUI.Data.OutputPoints.Remove(rOutputNode);
                list.index--;
                //移除连接线
                NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this.selfNodeGUI, Vector2.zero, rOutputNode));
                //刷新
                BDFrameworkAssetsEnv.UpdateNodeGraph(this.selfNodeGUI);
            }
        }

        private void AddToFilterEntryList(ReorderableList list)
        {
            AddOutputNode();
        }

        private void DrawFilterEntryListElement(Rect rect, int idx, bool isactive, bool isfocused)
        {
            var gp = this.groupFilterPathDataList[idx];
            //渲染数据
            bool isDisable = this.incommingAssetGroup.assetGroups.ContainsKey(gp.GroupPath);
            EditorGUI.BeginDisabledGroup(isDisable);


            var output = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width, rect.height * 0.9f), gp.GroupPath);
            //检测改动
            if (output != gp.GroupPath)
            {
                Debug.Log("改动:" + output);
                gp.GroupPath = output;
                //更新
                UpdateGroupPathData(idx);
                var outputConnect = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == gp.OutputNodeId);


                //BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, outputConnect);
                BDFrameworkAssetsEnv.UpdateNodeGraph(this.selfNodeGUI);
            }

            if (isDisable)
            {
                EditorGUI.EndDisabledGroup();
            }
        }

        private void OnChangeList(ReorderableList list)
        {
            Debug.Log("on change item list");

            //TODO 先排序让其他标签的为最低
            // redo node output due to filter condition change
        }


        private void ReorderFilterEntryList(ReorderableList list)
        {
            Debug.Log("recorder");
        }


        /// <summary>
        /// 添加
        /// </summary>
        private void AddOutputNode(string label = "")
        {
            if (string.IsNullOrEmpty(label))
            {
                label = (this.e_groupList.index + 1).ToString();
            }

            //不重复添加
            var ret = this.groupFilterPathDataList.Find((data) => data.GroupPath == label);
            if (ret != null)
            {
                return;
            }

            //添加输出节点
            this.e_groupList.index++;
            var node = this.selfNodeGUI.Data.AddOutputPoint(label);
            this.groupFilterPathDataList.Add(new GroupPathData() { OutputNodeId = node.Id, GroupPath = label });
        }


        /// <summary>
        /// 更新数据
        /// </summary>
        private void UpdateGroupPathData(int idx)
        {
            var gpd        = this.groupFilterPathDataList[idx];
            var outputNode = this.selfNodeGUI.Data.FindOutputPoint(gpd.OutputNodeId);
            outputNode.Label = gpd.GroupPath;
        }

        private PerformGraph.AssetGroups incommingAssetGroup = null;

        /// <summary>
        /// 刷新节点渲染
        /// </summary>

        #endregion

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
            Debug.Log("prepare:" + this.GetType().Name + "-" + DateTime.Now.ToLongTimeString());
            if (incoming == null)
            {
                return;
            }
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;

            //prepare传入的资源
            this.incommingAssetGroup = incoming.FirstOrDefault();

            //初始化输出节点
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    this.AddOutputNode(ag.Key);
                }
            }

            //初始化输出列表
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var group in this.groupFilterPathDataList)
            {
                if (!string.IsNullOrEmpty(group.GroupPath))
                {
                    outMap[group.GroupPath] = new List<AssetReference>();
                }
            }

            //把depend、runtime内包含的移除掉 
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    var assetList = group.Value.ToList();
                    for (int i = assetList.Count - 1; i >= 0; i--)
                    {
                        var assetRef = assetList[i];

                        foreach (var groupFilter in this.groupFilterPathDataList)
                        {
                            if (!string.IsNullOrEmpty(groupFilter.GroupPath))
                            {
                                //匹配路径
                                if (assetRef.importFrom.StartsWith(groupFilter.GroupPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    assetList.RemoveAt(i);
                                    //依次按分组输出
                                    outMap[groupFilter.GroupPath].Add(assetRef);
                                }
                            }
                        }
                    }

                    outMap[group.Key] = assetList;
                }
            }
            
            //一次
            if (connectionsToOutput != null)
            {
                foreach (var outpointNode in connectionsToOutput)
                {
                    var groupFilter = this.groupFilterPathDataList.FirstOrDefault((gf) => gf.OutputNodeId == outpointNode.FromNodeConnectionPointId);
                    if (groupFilter != null)
                    {
                        var kv = new Dictionary<string, List<AssetReference>>() { { groupFilter.GroupPath, outMap[groupFilter.GroupPath] } };
                        outputFunc(outpointNode, kv);
                    }
                }
            }
        }
    }
}