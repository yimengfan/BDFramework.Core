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
    public class SetGranularityByFolder : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 构建的上下文信息
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }

        public void Reset()
        {
        }


        /// <summary>
        /// 文件夹AB规则
        /// </summary>
        public enum FolderAssetBundleRule
        {
            /// <summary>
            /// 设置AB名为父文件夹路径
            /// </summary>
            用该目录路径设置AB名 = 1,

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

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
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


            //根据不同的枚举进行提示
            switch ((FolderAssetBundleRule) this.SetAssetBundleNameRule)
            {
                case FolderAssetBundleRule.用该目录路径设置AB名:
                {
                    EditorGUILayout.HelpBox("将该目录下所有文件,打包成一个AB!", MessageType.Info);
                }
                    break;

                case FolderAssetBundleRule.用子目录路径设置AB名:
                {
                    var label = "将该目录下所有子目录,按子目录分别打包AB! \n 如:A文件夹下有 A/a1,A/a2,A/a3三个文件夹, 则会打包为a1、a2、a3 三个AB包,\n 若存在类似A/1.txt文件, 额外会生成AssetBundle - A,包含1.txt";
                    EditorGUILayout.HelpBox(label, MessageType.Info);
                }
                    break;
            }

            if (isupdateNode)
            {
                Debug.Log("更新node!");
                //触发
                //BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, this.selfNodeGUI.Data.OutputPoints.FirstOrDefault());
                BDFrameworkAssetsEnv.UpdateNodeGraph(this.selfNodeGUI);
            }
        }

        /// <summary>
        /// 预览结果 编辑器连线数据，但是build模式也会执行
        /// 这里注意不要对BuildingCtx直接进行修改,修改需要在Build中进行
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
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;

            Debug.Log("prepare:" + this.GetType().Name + "-" + DateTime.Now.ToLongTimeString());



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
                    outMap = DoSetABNameUseSubfolderName(incoming);
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
                        var ret = this.BuildingCtx.BuildAssetsInfo.SetABName(ar.importFrom, folderPath);
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
        /// 将该目录下 所有子文件夹打包
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        private Dictionary<string, List<AssetReference>> DoSetABNameUseSubfolderName(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            var outMap = new Dictionary<string, List<AssetReference>>();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    var rootfloderPath = ag.Key;
                    if (!Directory.Exists(rootfloderPath))
                    {
                        continue;
                    }

                    //unity没有处理"/"，获取不到guid
                    if (rootfloderPath.EndsWith("/"))
                    {
                        rootfloderPath = rootfloderPath.Remove(rootfloderPath.Length - 1);
                    }

                    var rootguid = AssetDatabase.AssetPathToGUID(rootfloderPath);
                    Debug.Log($"父目录:{rootfloderPath} - {rootguid}");
                    //搜集子目录
                    var subfolders = Directory.GetDirectories(rootfloderPath, "*", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < subfolders.Length; i++)
                    {
                        var subFolder = subfolders[i];
                        var guid = AssetDatabase.AssetPathToGUID(subFolder);
                        outMap[subFolder] = new List<AssetReference>();
                        //打印文件夹hash
                        Debug.Log("子目录:" + subfolders[i] + " - " + guid);
                    }


                    //输出目录
                    foreach (var ar in ag.Value)
                    {
                        bool isInSubfolder = false;
                        //判断子目录
                        foreach (var sf in subfolders)
                        {
                            if (ar.importFrom.StartsWith(sf + "/", StringComparison.OrdinalIgnoreCase))
                            {
                                var ret =  this.BuildingCtx.BuildAssetsInfo.SetABName(ar.importFrom, sf);
                                if (!ret)
                                {
                                    Debug.LogError($"【颗粒度】设置AB失败 [{sf}] -" + ar.importFrom);
                                }

                                //添加到输出分组
                                outMap[sf].Add(ar);
                                isInSubfolder = true;
                                break;
                            }
                        }

                        //父目录//剩下全都打进父目录
                        if (!isInSubfolder)
                        {
                            //设置AB name
                            var ret =  this.BuildingCtx.BuildAssetsInfo.SetABName(ar.importFrom, rootfloderPath);
                            if (!ret)
                            {
                                Debug.LogError($"【颗粒度】设置AB失败 [{rootfloderPath}] -" + ar.importFrom);
                            }
                            
                            //根目录判断
                            if (!outMap.ContainsKey(rootfloderPath))
                            {
                                outMap[rootfloderPath] = new List<AssetReference>();
                            }
                            outMap[rootfloderPath].Add(ar);
                        }
                    }
                }
            }

            return outMap;
        }
    }
}
