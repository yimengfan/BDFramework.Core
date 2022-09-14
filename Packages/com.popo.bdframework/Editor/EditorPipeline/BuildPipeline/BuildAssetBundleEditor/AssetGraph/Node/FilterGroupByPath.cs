using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
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
            Regex,

            //通配符
            WildCard
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

        //
        // /// <summary>
        // /// 添加到List列表
        // /// </summary>
        // /// <param name="label"></param>
        // private void AddToList(string label)
        // {
        //     label = IPath.ReplaceBackSlash(label);
        //     var find = this.groupFilterPathDataList.FirstOrDefault((gf) => gf.GroupPath.Equals(label));
        //
        //     if (find == null)
        //     {
        //         this.groupFilterPathDataList.Add(new GroupPathData()
        //         {
        //             GroupPath = label,
        //         });
        //     
        //         this.AddOutputNode(label);
        //     }
        //     else
        //     {
        //         Debug.LogError("已经存在:" + label);
        //     }
        //
        //     
        //     
        // }


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
            //添加输出节点
            if (incommingAssetGroup != null)
            {
                foreach (var ag in incommingAssetGroup.assetGroups)
                {
                    this.AddOutputNode(ag.Key);
                }
            }


            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("添加前置节点", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    if (this.groupFilterPathDataList.Count == 0)
                    {
                        AddOutputNode(nameof(BDFrameworkAssetsEnv.FloderType.Runtime));
                        AddOutputNode(nameof(BDFrameworkAssetsEnv.FloderType.Depend));
                        AddOutputNode(nameof(BDFrameworkAssetsEnv.FloderType.Shaders));
                        AddOutputNode(nameof(BDFrameworkAssetsEnv.FloderType.SpriteAtlas));
                    }
                }

                //
                if (GUILayout.Button("添加Runtime目录", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    foreach (var runtimeDir in BApplication.GetAllRuntimeDirects())
                    {
                        var dirs = Directory.GetDirectories(runtimeDir, "*", SearchOption.TopDirectoryOnly);
                        foreach (var dir in dirs)
                        {
                            AddOutputNode(dir);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();


            GUILayout.Space(10);
            //选择模式
            var curSelect = (CompareModeEnum) EditorGUILayout.EnumPopup("路径匹配模式", this.CompareMode);
            if (curSelect != this.CompareMode)
            {
                isDirty = true;
                this.CompareMode = curSelect;
            }

            //渲染列表
            //初始化group list
            InitGroupList();
            GUILayout.Label("路径匹配:建议以\"/\"结尾,不然路径中包含这一段path都会被匹配上.");
            e_groupList.DoLayoutList();


            //
            if (isDirty)
            {
                Debug.Log("更新node!");
                //触发
                //BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, this.selfNodeGUI.Data.OutputPoints.FirstOrDefault());
                AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
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
                Debug.Log("select：" + list.index);
                //移除序列化值
                var removeIdx = list.index;
                var removeItem = this.groupFilterPathDataList[removeIdx];

                if (EditorUtility.DisplayDialog("提示", "是否删除 " + removeItem.GroupPath, "OK", "Cancel"))
                {
                    this.groupFilterPathDataList.RemoveAt(removeIdx);
                    //移除输出节点
                    var rOutputNode = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == removeItem.OutputNodeId);
                    this.selfNodeGUI.Data.OutputPoints.Remove(rOutputNode);
                    //刷新
                    AssetGraphTools.RemoveOutputNode(this.selfNodeGUI, rOutputNode);
                    AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
                }
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
            bool isDisable = false;
            if (this.incommingAssetGroup != null)
            {
                isDisable = this.incommingAssetGroup.assetGroups.ContainsKey(gp.GroupPath);
            }
            else
            {
                if (gp.GroupPath == nameof(BDFrameworkAssetsEnv.FloderType.Runtime) || gp.GroupPath == nameof(BDFrameworkAssetsEnv.FloderType.Depend) ||
                    gp.GroupPath == nameof(BDFrameworkAssetsEnv.FloderType.Shaders) || gp.GroupPath == nameof(BDFrameworkAssetsEnv.FloderType.SpriteAtlas))
                {
                    isDisable = true;
                }
            }

            EditorGUI.BeginDisabledGroup(isDisable);
            var editorInput = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width, rect.height * 0.9f), gp.GroupPath);
            //检测改动
            if (editorInput != gp.GroupPath)
            {
                gp.GroupPath = editorInput;
                //更新
                UpdateGroupPathData(idx);
                //var outputConnect = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == gp.OutputNodeId);
                //BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, outputConnect);
                AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
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

            UpdateListToOutput();
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
            else
            {
                if (Directory.Exists(label))
                {
                    label = IPath.AddEndSymbol(label);
                    label = IPath.ReplaceBackSlash(label);
                }
            }


            //不重复添加
            var ret = this.groupFilterPathDataList.Find((data) => data.GroupPath == label);
            if (ret != null)
            {
                return;
            }

            Debug.Log("添加目录:" + label);
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
            var AssetGraphEditor = AssetGraphEditorWindow.Window;
            //
            var gpd = this.groupFilterPathDataList[idx];
            var outputPoint = this.selfNodeGUI.Data.FindOutputPoint(gpd.OutputNodeId);
            gpd.GroupPath = IPath.ReplaceBackSlash(gpd.GroupPath);
            outputPoint.Label = gpd.GroupPath;
            //

            var con = AssetGraphEditor.Connections.FirstOrDefault((c) => c.OutputPoint.Id == outputPoint.Id);
            if (con != null)
            {
                con.Label = gpd.GroupPath;
            }
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
//                e_groupList.onMouseDragCallback += (ReorderableList list) => { Debug.Log(list.index); };

                e_groupList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
                e_groupList.headerHeight = 3;
                e_groupList.index = this.groupFilterPathDataList.Count - 1;
                e_groupList.draggable = true;
            }
        }

        /// <summary>
        /// 刷新节点渲染
        /// </summary>

        #endregion

        /// <summary>
        /// 更新列表到输出节点
        /// </summary>
        private void UpdateListToOutput()
        {
            bool isChanged = false;
            var AssetGraphEditor = AssetGraphEditorWindow.Window;
            for (int i = 0; i < e_groupList.list.Count; i++)
            {
                var gp = e_groupList.list[i] as GroupPathData;
                var idx = this.selfNodeGUI.Data.OutputPoints.FindIndex((point) => point.Label.Equals(gp.GroupPath));

                //发生了变化
                if (i != idx && idx >= 0 && this.selfNodeGUI.Data.OutputPoints.Count > i)
                {
                    isChanged = true;
                    var curOutputPoint = this.selfNodeGUI.Data.OutputPoints[i];
                    var lastOutputPoint = this.selfNodeGUI.Data.OutputPoints[idx];
                    //交换连接线
                    var lastCon = AssetGraphEditor.Connections.FirstOrDefault((con) => con.OutputPoint.Id == lastOutputPoint.Id);
                    var curCon = AssetGraphEditor.Connections.FirstOrDefault((con) => con.OutputPoint.Id == curOutputPoint.Id);


                    //将线连至当前,
                    if (lastCon != null)
                    {
                        var endNode1 = AssetGraphEditor.Nodes.FirstOrDefault((node) => node.Id == lastCon.Data.ToNodeId);
                        AssetGraphEditor.AddConnection(gp.GroupPath, this.selfNodeGUI, curOutputPoint, endNode1, lastCon.InputPoint);
                    }
                    else if (curCon != null)
                    {
                        curCon.Delete();
                    }

                    //将原来当前的线换位
                    if (curCon != null)
                    {
                        var endNode2 = AssetGraphEditor.Nodes.FirstOrDefault((node) => node.Id == curCon.Data.ToNodeId);
                        AssetGraphEditor.AddConnection(lastOutputPoint.Label, this.selfNodeGUI, lastOutputPoint, endNode2, curCon.InputPoint);
                    }
                    else if (lastCon != null)
                    {
                        lastCon.Delete();
                    }


                    lastOutputPoint.Label = curOutputPoint.Label;
                    curOutputPoint.Label = gp.GroupPath;
                    //输出节点重复制
                    gp.OutputNodeId = curOutputPoint.Id;
                }
                //
            }

            //重新整理
            for (int i = 0; i < e_groupList.count; i++)
            {
                var gp = e_groupList.list[i] as GroupPathData;
                var outputPoint = this.selfNodeGUI.Data.OutputPoints[i];
                gp.OutputNodeId = outputPoint.Id;
                outputPoint.Label = gp.GroupPath;
                //
                var con = AssetGraphEditor.Connections.FirstOrDefault((c) => c.OutputPoint.Id == outputPoint.Id);
                if (con != null)
                {
                    con.Label = gp.GroupPath;
                }
            }

            if (isChanged)
            {
                AssetGraphEditor.Setup();
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
                bool isRemove = false;
                foreach (var ags in incoming)
                {
                    foreach (var ag in ags.assetGroups)
                    {
                        if (gf.GroupPath == ag.Key)
                        {
                            groupFilterPatList.Remove(gf);
                            break;
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

                    if (outMap.ContainsKey(group.Key))
                    {
                        outMap[group.Key].AddRange(assetList);
                    }
                    else
                    {
                        outMap[group.Key] = assetList;
                    }
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
            else
            {
                Debug.Log($"{this.Category}: 输入：{inputCount}  输出：{outputCount}");
            }


            //判断有没有连线
            var AssetGraphEditor = AssetGraphEditorWindow.Window;
            if (AssetGraphEditor!=null && AssetGraphEditor.Connections != null)
            {
                foreach (var gf in this.groupFilterPathDataList)
                {
                    var con = AssetGraphEditor.Connections.FirstOrDefault((c) => c?.OutputPoint?.Id == gf.OutputNodeId);

                    if (con == null)
                    {
                        Debug.Log($"<color=yellow>{this.Category} 注意！！！！有节点没有连接输出：{gf.GroupPath}</color>");
                    }
                }
            }

            //一次
            if (connectionsToOutput != null)
            {
                //
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
                        Debug.LogError($" 找不到输出节点:{outpointNode.Label} - {outpointNode.FromNodeConnectionPointId}");
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
                case CompareModeEnum.WildCard:
                {
                    var split = group.ToLower().Split(Settings.KEYWORD_WILDCARD);
                    var groupingKeywordPrefix = split[0];
                    var groupingKeywordPostfix = split[1];
                    Regex regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
                    return regex.IsMatch(path.ToLower());
                }
                    break;
            }

            return false;
        }
    }
}
