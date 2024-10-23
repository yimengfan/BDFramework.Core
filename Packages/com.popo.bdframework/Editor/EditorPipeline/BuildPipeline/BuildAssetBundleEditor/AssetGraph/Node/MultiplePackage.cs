using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.VersionController;
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
    [CustomNode("BDFramework/[分包]资源路径分包", 110)]
    public class MultiplePackage : UnityEngine.AssetGraph.Node
    {


        /// <summary>
        /// 分包配置表
        /// </summary>
        static public List<SubPackageConfigItem> AssetMultiplePackageConfigList = new List<SubPackageConfigItem>();

        public void Reset()
        {
        }

        public override string ActiveStyle
        {
            get { return "node 3 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 3"; }
        }

        public override string Category
        {
            get { return "[分包]Set by Path"; }
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
        public override void OnDrawNodeGUIContent(NodeGUI node)
        {
            this.selfNodeGUI = node;
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
          
            //初始化group list
            if (e_groupList == null)
            {
                e_groupList = new ReorderableList(groupFilterPathDataList, typeof(string), false, false, true, true);
                e_groupList.onReorderCallback = ReorderFilterEntryList;
                e_groupList.onAddCallback = AddToFilterEntryList;
                e_groupList.onRemoveCallback = RemoveFromFilterEntryList;
                e_groupList.drawElementCallback = DrawFilterEntryListElement;
                // e_groupList.onCanRemoveCallback = (list) =>
                // {
                //     if (e_groupList.count <= 2)
                //     {
                //         return false;
                //     }
                //
                //     return true;
                // };
                e_groupList.onChangedCallback = OnChangeList;
                e_groupList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
                e_groupList.headerHeight = 3;
                e_groupList.index = this.groupFilterPathDataList.Count - 1;
            }

            //添加输出节点
            // if (incommingAssetGroup != null)
            // {
            //     foreach (var ag in incommingAssetGroup.assetGroups)
            //     {
            //         this.AddOutputNode(ag.Key);
            //     }
            // }


            GUILayout.Label("1.路径匹配:建议以\"/\"结尾,不然路径中包含这一段path都会被匹配上.");
            GUILayout.Label("2.分包路径原则上需要包含/Runtime/");
            e_groupList.DoLayoutList();
        }


        private void RemoveFromFilterEntryList(ReorderableList list)
        {
            if (list.count > 0)
            {
                //移除序列化值
                var removeIdx = this.groupFilterPathDataList.Count - 1;
                var rItem = this.groupFilterPathDataList[removeIdx];
                this.groupFilterPathDataList.RemoveAt(removeIdx);
                //移除输出节点
                var rOutputNode = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == rItem.OutputNodeId);
                this.selfNodeGUI.Data.OutputPoints.Remove(rOutputNode);
                list.index--;
                //移除连接线
                NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this.selfNodeGUI, Vector2.zero, rOutputNode));
                //刷新
                AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
            }
        }

        private void AddToFilterEntryList(ReorderableList list)
        {
            AddOutputNode();
        }

        private void DrawFilterEntryListElement(Rect rect, int idx, bool isactive, bool isfocused)
        {
            var gp = this.groupFilterPathDataList[idx];

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
                AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
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
            this.groupFilterPathDataList.Add(new GroupPathData() {OutputNodeId = node.Id, GroupPath = label});
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

            //
            AssetMultiplePackageConfigList = new List<SubPackageConfigItem>();
            //prepare传入的资源
            this.incommingAssetGroup = incoming.FirstOrDefault();
           // this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;
           //this.BuildParams = BDFrameworkAssetsEnv.BuildParams;
            //初始化输出列表
            var outMap = new Dictionary<string, Dictionary<string, List<AssetReference>>>();
            foreach (var group in this.groupFilterPathDataList)
            {
                if (!string.IsNullOrEmpty(group.GroupPath))
                {
                    outMap[group.GroupPath] = new Dictionary<string, List<AssetReference>>();
                }
            }

            var assetABNameList = new List<string>();
            var assetRefenceList = new List<AssetReference>();
            //buildAssetbundle节点传过来是 abname = arlist 这样的结构.
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    assetABNameList.Add(group.Key);
                    assetRefenceList.AddRange(group.Value);
                }
            }
            // //遍历分组
            // foreach (var groupFilter in this.groupFilterPathDataList)
            // {
            //     outMap[groupFilter.GroupPath].Add(assetPath, new List<AssetReference>(group.Value));
            // }

            //
            foreach (var abname in assetABNameList)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(abname);
                //遍历分组
                foreach (var groupFilter in this.groupFilterPathDataList)
                {
                    //匹配路径分组
                    if (!string.IsNullOrEmpty(groupFilter.GroupPath))
                    {
                        if (assetPath.StartsWith(groupFilter.GroupPath, StringComparison.OrdinalIgnoreCase))
                        {
                            //查询这一组资源
                            foreach (var ags in incoming)
                            {
                                foreach (var group in ags.assetGroups)
                                {
                                    if (group.Key.Equals(abname, StringComparison.OrdinalIgnoreCase))
                                    {
                                        //这里是以前面传来的 分组颗粒进行添加
                                        outMap[groupFilter.GroupPath].Add(assetPath, new List<AssetReference>(group.Value));
                                    }
                                }
                            }
                        }
                    }
                }
            }


            //一次
            if (connectionsToOutput != null)
            {
                foreach (var outpointNode in connectionsToOutput)
                {
                    var gf = this.groupFilterPathDataList.FirstOrDefault((gpd) => gpd.OutputNodeId == outpointNode.FromNodeConnectionPointId);
                    if (gf != null)
                    {
                        outputFunc(outpointNode, outMap[gf.GroupPath]);
                    }
                }
            }
        }
    }
}
