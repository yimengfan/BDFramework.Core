using System;
using System.Collections.Generic;
using System.Linq;
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
    public class FilterGroupByPath : UnityEngine.AssetGraph.Node, IBDAssetBundleV2Node
    {
        public BuildInfo BuildInfo { get; set; }

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
        /// </summary>
        [SerializeField] public List<GroupPathData> groupFilterPathDataList = new List<GroupPathData>();

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
            if (e_groupList == null)
            {
                e_groupList = new ReorderableList(groupFilterPathDataList, typeof(string), true, false, true, true);
                e_groupList.onReorderCallback = ReorderFilterEntryList;
                e_groupList.onAddCallback = AddToFilterEntryList;
                e_groupList.onRemoveCallback = RemoveFromFilterEntryList;
                e_groupList.drawElementCallback = DrawFilterEntryListElement;
                e_groupList.onChangedCallback = OnChangeList;
                e_groupList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
                e_groupList.headerHeight = 3;

                e_groupList.index = this.groupFilterPathDataList.Count - 1;
            }

            this.selfNodeGUI = node;

            GUILayout.Label("路径匹配:建议以\"/\"结尾,不然路径中包含这一段path都会被匹配上.");
            e_groupList.DoLayoutList();
        }


        private void RemoveFromFilterEntryList(ReorderableList list)
        {
            //使用scope能触发刷新
            // using (new RecordUndoScope("Remove Group Condition", this.selfNodeGUI))
            // {
            if (list.index > 0)
            {
                this.groupFilterPathDataList.RemoveAt(this.groupFilterPathDataList.Count - 1);
                list.index--;
                list.onChangedCallback.Invoke(list);
            }
            // }
        }

        private void AddToFilterEntryList(ReorderableList list)
        {
            //使用scope能触发刷新
            // using (new RecordUndoScope("Add Group Condition", this.selfNodeGUI))
            // {
            list.index++;
            var node = this.selfNodeGUI.Data.AddOutputPoint(list.index.ToString());
            this.groupFilterPathDataList.Add(new GroupPathData()
            {
                OutputNodeId = node.Id,
                GroupPath = list.index.ToString()
            });
            // }
        }

        private void DrawFilterEntryListElement(Rect rect, int idx, bool isactive, bool isfocused)
        {
            //渲染数据
            var gp = this.groupFilterPathDataList[idx];
            gp.GroupPath = EditorGUILayout.TextField(gp.GroupPath);
            //更新
            UpdateGroupPathData(idx);
        }

        private void OnChangeList(ReorderableList list)
        {
            Debug.Log("onchangelist");
            
            //TODO 先排序让其他标签的为最低
            // redo node output due to filter condition change
            
            
            
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_UPDATED, this.selfNodeGUI));
        }


        private void ReorderFilterEntryList(ReorderableList list)
        {
            Debug.Log("recorder");
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        private void UpdateGroupPathData(int idx)
        {
            var gpd = this.groupFilterPathDataList[idx];
            var outputNode = this.selfNodeGUI.Data.FindOutputPoint(gpd.OutputNodeId);
            outputNode.Label = gpd.GroupPath;
        }

        #endregion


        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }

            this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;

            //初始化输出列表
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var group in this.groupFilterPathDataList)
            {
                if (!string.IsNullOrEmpty(group.GroupPath))
                {
                    outMap[group.GroupPath] = new List<AssetReference>();
                }
            }

            //在depend 和runtime内进行筛选
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    if (group.Key == nameof(BDFrameworkAssetsEnv.FloderType.Runtime) || group.Key == nameof(BDFrameworkAssetsEnv.FloderType.Depend))
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
                                        //添加到输出
                                        outMap[groupFilter.GroupPath].Add(assetRef);
                                    }
                                }
                            }
                        }

                        outMap[group.Key] = assetList;
                    }
                }
            }


            //输出
            if (connectionsToOutput != null)
            {
                foreach (var outpointNode in connectionsToOutput)
                {
                    var groupFilter = this.groupFilterPathDataList.FirstOrDefault((gf) => gf.OutputNodeId == outpointNode.FromNodeConnectionPointId);
                    if (groupFilter != null)
                    {
                        var kv = new Dictionary<string, List<AssetReference>>()
                        {
                            {groupFilter.GroupPath, outMap[groupFilter.GroupPath]}
                        };
                        outputFunc(outpointNode, kv);
                    }
                }
            }
        }
    }
}
