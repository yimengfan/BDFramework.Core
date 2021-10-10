using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using BDFramework.Editor.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,排序30-50
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]文件夹规则", 30)]
    public class SetGranularityByFolder : UnityEngine.AssetGraph.Node, IBDFrameowrkAssetEnvParams
    {
        public BuildInfo BuildInfo { get; set; }
        public BuildAssetBundleParams BuildParams { get; set; }


        /// <summary>
        /// 文件夹AB规则
        /// </summary>
        public enum FolderAssetBundleRule
        {
            /// <summary>
            /// 设置AB名为父文件夹路径
            /// </summary>
            用该目录路径设置AB名,

            /// <summary>
            /// 设置所有子文件夹中的文件，AB名为子文件夹名
            /// </summary>
            用子目录路径设置AB名
        }

        /// <summary>
        /// 设置规则
        /// 这里的值一定要public，不然sg 用json序列化判断值未变化，则不会刷新
        /// </summary>
        public int SetAssetBundleNameRule = (int) FolderAssetBundleRule.用该目录路径设置AB名;


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
            get { return "[颗粒度]文件夹规则"; }
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

        private NodeGUI selfNodeGUI;
        private Action onInspectorValueChanged;

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            this.onInspectorValueChanged = onValueChanged;
            this.selfNodeGUI = node;
            //node.Name                 = EditorGUILayout.TextField("Tips:",  node.Name);
            editor.UpdateNodeName(node);
            if (!node.Name.StartsWith("[颗粒度]"))
            {
                node.Name = "[颗粒度]" + node.Name;
            }

            bool isupdateNode = false;

            //包装一层 方便监听改动
            var ret = EditorGUILayout.EnumPopup("设置规则", (FolderAssetBundleRule) this.SetAssetBundleNameRule).GetHashCode();

            if (ret != this.SetAssetBundleNameRule)
            {
                this.SetAssetBundleNameRule = ret;
                isupdateNode = true;
            }

            if (isupdateNode)
            {
                //触发
                BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, this.selfNodeGUI.Data.OutputPoints.FirstOrDefault());
                BDFrameworkAssetsEnv.UpdateNodeGraph(this.selfNodeGUI);
            }
        }

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            Debug.Log("刷新值!" + DateTime.Now.ToLongTimeString());
            if (incoming == null)
            {
                return;
            }

            if (this.BuildInfo == null)
            {
                this.BuildInfo = BDFrameworkAssetsEnv.BuildInfo;
            }

            if (this.BuildParams == null)
            {
                this.BuildParams = BDFrameworkAssetsEnv.BuildParams;
            }

            //
            var outMap = new Dictionary<string, List<AssetReference>>();
            switch ((FolderAssetBundleRule) this.SetAssetBundleNameRule)
            {
                case FolderAssetBundleRule.用该目录路径设置AB名:
                {
                    outMap = SetABNameUseThisFolderName(incoming);
                }
                    break;

                case FolderAssetBundleRule.用子目录路径设置AB名:
                {
                    outMap = DoSetABNameUseSubFolderName(incoming);
                }
                    break;
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
        private Dictionary<string, List<AssetReference>> SetABNameUseThisFolderName(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    var folderPath = ag.Key;

                    foreach (var ar in ag.Value)
                    {
                        //设置当前ab名为文件夹名,不覆盖在此之前的规则
                        var ret = BuildInfo.SetABName(ar.importFrom, folderPath);
                        if (!ret)
                        {
                            Debug.LogError($"【颗粒度】设置AB失败 [{folderPath}] -" + ar.importFrom);
                        }
                    }

                    //添加到输出分组
                    outMap[ag.Key] = ag.Value.ToList();
                }
            }

            return outMap;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        private Dictionary<string, List<AssetReference>> DoSetABNameUseSubFolderName(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    var floderPath = ag.Key;
                    var subFolders = Directory.GetDirectories(floderPath, "", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < subFolders.Length; i++)
                    {
                        subFolders[i] = subFolders[i] + "/";

                        var guid = AssetDatabase.AssetPathToGUID(subFolders[i]);
                        Debug.Log(guid);
                    }

                    foreach (var ar in ag.Value)
                    {
                        //设置ab名为子目录名,不覆盖在此之前的规则
                        foreach (var subFolder in subFolders)
                        {
                            if (ar.importFrom.StartsWith(subFolder))
                            {
                                var ret = BuildInfo.SetABName(ar.importFrom, subFolder);
                                if (!ret)
                                {
                                    Debug.LogError($"【颗粒度】设置AB失败 [{subFolder}] -" + ar.importFrom);
                                }

                                //添加到输出分组
                                outMap[subFolder].Add(ar);
                                break;
                            }
                        }
                    }
                }
            }

            return outMap;
        }
    }
}
