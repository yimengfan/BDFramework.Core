using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Versioning;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 筛选,通过输出所有子目录
    /// </summary>
    [CustomNode("BDFramework/[分组]Group SubDirectory", 10)]
    public class FilterGroupSplitDirectory : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 构建的上下文信息
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }


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
            get { return "[筛选]Group SubDirectory"; }
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

        #region 渲染

        private NodeGUI selfNodeGUI;

        /// <summary>
        /// 绘制NodeGUI
        /// </summary>
        /// <param name="node"></param>
        public override void OnDrawNodeGUIContent(NodeGUI node)
        {
            this.selfNodeGUI = node;
        }
        
        /// <summary>
        /// 绘制inspector
        /// </summary>
        /// <param name="node"></param>
        /// <param name="streamManager"></param>
        /// <param name="inspector"></param>
        /// <param name="onValueChanged"></param>
        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
         
            EditorGUILayout.HelpBox("该节点，用于分组 传入路径的所有子目录，默认只接受一个传入!", MessageType.Info);
        }

        public override void OnContextMenuGUI(GenericMenu menu)
        {
            base.OnContextMenuGUI(menu);
        }

        private void InitOutputNode()
        {
        }

        /// <summary>
        /// 添加
        /// </summary>
        private void InitOutputNode(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            //只获取第一个输出
            var item = incoming.FirstOrDefault().assetGroups.FirstOrDefault();

            //第一个key作为路径
            if (!Directory.Exists(item.Key))
            {
                Debug.LogError("不存在路径:" + item.Key);
                return;
            }

            //路径
            var rootDir = item.Key;
            var subDirList = Directory.GetDirectories(rootDir, "*", SearchOption.TopDirectoryOnly).ToList();
            
            //根目录下存在资产
            var rootDirFiles = Directory.GetFiles(rootDir, "*", SearchOption.TopDirectoryOnly).Where((d)=>!d.EndsWith(".meta"));
            if (rootDirFiles.Count() >= 2)
            {
                subDirList.Add(rootDir);
            }

            //添加结束符
            for (int i = 0; i < subDirList.Count; i++)
            {
                subDirList[i] = IPath.AddEndSymbol(subDirList[i]);
            }

            //删除不存在的
            for (int i =  this.groupFilterPathDataList.Count-1; i >=0; i--)
            {
                var gp = this.groupFilterPathDataList[0];
                if (!subDirList.Contains(gp.GroupPath))
                {
                    this.RemoveOutputNode(gp.OutputNodeId);
                }
            }
    

            //添加目录
            foreach (var dir in subDirList)
            {
                var ret = this.groupFilterPathDataList.Find((gf) => gf.GroupPath == dir);

                if (ret == null)
                {
                    this.AddOutputNode(dir);
                }
            }
        }

        /// <summary>
        /// 移除输出节点
        /// </summary>
        /// <param name="uIdx"></param>
        private void RemoveOutputNode(string uIdx)
        {
            //移除输出节点
            var outputNode = this.selfNodeGUI.Data.OutputPoints.Find((node) => node.Id == uIdx);
            //移除数据
            var gp= this.groupFilterPathDataList.Find((g) => g.OutputNodeId.Equals(uIdx));
            this.groupFilterPathDataList.Remove(gp);
            //移除连接线
            AssetGraphTools.RemoveOutputNode(this.selfNodeGUI, outputNode);
            AssetGraphTools.UpdateConnectLine(this.selfNodeGUI, outputNode);
        }


        /// <summary>
        /// 更新数据
        /// </summary>
        private void AddOutputNode(string path)
        {
            var ret = this.groupFilterPathDataList.Find((g) => g.GroupPath.Equals(path));
            if (ret != null)
            {
                return;
            }

            if (this.selfNodeGUI != null)
            {
                var node = this.selfNodeGUI.Data.AddOutputPoint(path);
                this.groupFilterPathDataList.Add(new GroupPathData()
                {
                    GroupPath = path,
                    OutputNodeId = node.Id
                });
                AssetGraphTools.UpdateConnectLine(this.selfNodeGUI, node);
            }

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
            //prepare传入的资源
            this.InitOutputNode(incoming);
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;
            
            //搜集所有的 asset reference 
            var comingAssetReferenceList = AssetGraphTools.GetComingAssets(incoming);
            if (comingAssetReferenceList.Count == 0)
            {
                return;
            }
            var assetsList = AssetGraphTools.GetComingAssets(incoming);
            //初始化输出列表
            var allOutMap = new Dictionary<string, List<AssetReference>>();
            foreach (var group in this.groupFilterPathDataList)
            {
                if (!string.IsNullOrEmpty(group.GroupPath))
                {
                    allOutMap[group.GroupPath] = new List<AssetReference>();
                }
            }

            //遍历输出
            foreach (var ar in assetsList)
            {
                foreach (var gp in groupFilterPathDataList)
                {
                    if (ar.importFrom.StartsWith(gp.GroupPath,StringComparison.OrdinalIgnoreCase))
                    {
                        //添加
                        allOutMap[gp.GroupPath].Add(ar);
                        break;
                    }
                }
            }

            //每个节点输出
            if (connectionsToOutput != null)
            {
                foreach (var outputNode in connectionsToOutput)
                {
                    var groupFilter = this.groupFilterPathDataList.FirstOrDefault((gf) => gf.OutputNodeId == outputNode.FromNodeConnectionPointId);
                    if (groupFilter != null)
                    {
                        var kv = new Dictionary<string, List<AssetReference>>() {{groupFilter.GroupPath, allOutMap[groupFilter.GroupPath]}};
                        outputFunc(outputNode, kv);
                    }
                }
            }
        }
    }
}
