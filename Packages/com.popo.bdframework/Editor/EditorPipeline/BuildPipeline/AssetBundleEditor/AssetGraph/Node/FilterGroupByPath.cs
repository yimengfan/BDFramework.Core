using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BDFramework.Editor.AssetBundle;
using BDFramework.StringEx;
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
    [CustomNode("BDFramework/[分组]Group by path", 10)]
    public class FilterGroupByPath : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 匹配模式
        /// </summary>
        public enum CompareModeEnum
        {
            //以该路径开始
            StartWith,

            //包含
            Contains,

            //正则
            Regex
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
            get { return "[筛选]Group by path"; }
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
        /// 比较模式
        /// </summary>
        public CompareModeEnum CompareMode = CompareModeEnum.StartWith;

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
            //初始化group list
            InitGroupList();
            //添加输出口
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

        /// <summary>
        /// 是否修改数据
        /// </summary>
        private bool isDirty;
        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            //初始化group list
            InitGroupList();

            //添加输出节点
            if (incommingAssetGroup != null)
            {
                foreach (var ag in incommingAssetGroup.assetGroups)
                {
                    this.AddOutputNode(ag.Key);
                }
            }

            //选择模式
            var curSelect = (CompareModeEnum) EditorGUILayout.EnumPopup("路径匹配模式", this.CompareMode);
            if (curSelect != this.CompareMode)
            {
                isDirty = true;
                this.CompareMode = curSelect;
            }
            //
            GUILayout.Label("路径匹配:建议以\"/\"结尾,不然路径中包含这一段path都会被匹配上.");
            e_groupList.DoLayoutList();
            
            
            //
            if (isDirty)
            {
                Debug.Log("更新node!");
                //触发
                //BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, this.selfNodeGUI.Data.OutputPoints.FirstOrDefault());
                GraphNodeHelper.UpdateNodeGraph(this.selfNodeGUI);
            }
        }


        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="list"></param>
        private void RemoveFromFilterEntryList(ReorderableList list)
        {
            if (list.count > 1)
            {
                //移除序列化值
                var removeIdx = this.groupFilterPathDataList.Count - 1;
                var rItem = this.groupFilterPathDataList[removeIdx];
                this.groupFilterPathDataList.RemoveAt(removeIdx);
                //移除输出节点
                var rOutputNode = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == rItem.OutputNodeId);
                this.selfNodeGUI.Data.OutputPoints.Remove(rOutputNode);
                list.index--;
                //刷新
                GraphNodeHelper.RemoveOutputNode(this.selfNodeGUI, rOutputNode);
                GraphNodeHelper.UpdateNodeGraph(this.selfNodeGUI);
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
                GraphNodeHelper.UpdateNodeGraph(this.selfNodeGUI);
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
        /// 初始化Group
        /// </summary>
        private void InitGroupList()
        {
            if (e_groupList == null)
            {
                e_groupList = new ReorderableList(groupFilterPathDataList, typeof(string), false, false, true, true);
                e_groupList.onReorderCallback = ReorderFilterEntryList;
                e_groupList.onAddCallback = AddToFilterEntryList;
                e_groupList.onRemoveCallback = RemoveFromFilterEntryList;
                e_groupList.drawElementCallback = DrawFilterEntryListElement;
                e_groupList.onCanRemoveCallback = (list) =>
                {
                    if (e_groupList.count <= 1)
                    {
                        return false;
                    }

                    return true;
                };
                e_groupList.onChangedCallback = OnChangeList;
                e_groupList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
                e_groupList.headerHeight = 3;
                e_groupList.index = this.groupFilterPathDataList.Count - 1;
            }
        }

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
            var outMap = new Dictionary<string, List<AssetReference>>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in this.groupFilterPathDataList)
            {
                if (!string.IsNullOrEmpty(group.GroupPath))
                {
                    outMap[group.GroupPath] = new List<AssetReference>();
                }
            }

            

            //1.前面传入的分组不变，新建的分组都会从 上面的分组中移除
            var groupFilterPatList = new List<GroupPathData>(this.groupFilterPathDataList);
            for (int i = groupFilterPatList.Count - 1; i >= 0; i--)
            {
                var gf = groupFilterPatList[i];
                //
                foreach (var ags in incoming)
                {
                    foreach (var ag in ags.assetGroups)
                    {
                        if (gf.GroupPath == ag.Key)
                        {
                            groupFilterPatList.RemoveAt(i);
                        }
                    }
                }
            }
            //排序
            groupFilterPatList.Sort((a, b) =>
            {
                if (a.GroupPath.Length > b.GroupPath.Length)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            });

            //开始遍历
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    var test = group.Value.Select((g)=>g.importFrom).ToList();
                    var assetList = group.Value.ToList();
                    
                    for (int i = assetList.Count - 1; i >= 0; i--)
                    {
                        var assetRef = assetList[i];

                        //根据路径进行分组
                        for (int j = 0; j < groupFilterPatList.Count; j++)
                        {
                            var gf = groupFilterPatList[j];
                            if (!string.IsNullOrEmpty(gf.GroupPath))
                            {
                                if (ComparePath(assetRef.importFrom, gf.GroupPath))
                                {
                                    assetList.RemoveAt(i);
                                    //依次按分组输出
                                    outMap[gf.GroupPath].Add(assetRef);
                                    break;
                                }
                            }
                        }
                    }
                    outMap[group.Key] = assetList;
                }
            }
            //校验
            int inputCount = 0;
            int outputCount = 0;
            foreach (var ags in incoming)
            {
                foreach (var group in ags.assetGroups)
                {
                    inputCount += group.Value.Count;
                }
            }

            foreach (var outList in outMap)
            {
                outputCount += outList.Value.Count;
            }

            if (inputCount != outputCount)
            {
                throw new Exception($"【GroupByPath】分组输入输出不相等! input: {inputCount} output: {outputCount}");
            }



            //一次
            if (connectionsToOutput != null)
            {
                foreach (var outpointNode in connectionsToOutput)
                {
                    var groupFilter = this.groupFilterPathDataList.FirstOrDefault((gf) => gf.OutputNodeId == outpointNode.FromNodeConnectionPointId || gf.GroupPath.Equals(outpointNode.Label));
                    if (groupFilter != null)
                    {
                        var kv = new Dictionary<string, List<AssetReference>>() {{groupFilter.GroupPath, outMap[groupFilter.GroupPath]}};
                        outputFunc(outpointNode, kv);
                    }
                    else
                    {
                        Debug.LogError($" 找不到输出节点:{ outpointNode.Label} - {outpointNode.FromNodeConnectionPointId}");
                    }
                }
            }
        } 


        /// <summary>
        /// 检测路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ComparePath(string path, string group)
        {
            switch (this.CompareMode)
            {
                case CompareModeEnum.StartWith:
                {
                    return path.StartsWith(group, StringComparison.OrdinalIgnoreCase);
                }
                    break;
                case CompareModeEnum.Contains:
                {
                    return path.Contains(group, StringComparison.OrdinalIgnoreCase);
                }
                case CompareModeEnum.Regex:
                {
                    Regex regex = new Regex(group.ToLower());
                    return regex.IsMatch(path.ToLower());
                }
                    break;
            }

            return false;
        }
    }
}