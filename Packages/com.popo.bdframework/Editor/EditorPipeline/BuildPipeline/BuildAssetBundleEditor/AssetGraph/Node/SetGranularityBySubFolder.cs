using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 颗粒度,排序30-50
    /// </summary>
    [CustomNode("BDFramework/[颗粒度]按子文件夹打包", 31)]
    public class SetGranularityBySubFolder : SetGranularityBase
    {
        /// <summary>
        /// 打包类型
        /// </summary>
        public enum MakePackTypeEnum
        {
            所有子文件夹,
            通配符匹配
        }

        //打包类型
        public int MakePackType = (int) MakePackTypeEnum.所有子文件夹;

        /// <summary>
        /// 匹配路径
        /// </summary>
        public string MatchPath;

        public override string ActiveStyle
        {
            get { return "node 4 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 4"; }
        }

        public override string Category
        {
            get { return "[颗粒度]按子文件夹打包"; }
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
            return new SetGranularityBySubFolder();
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
        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            bool isdirty = false;

            var select = EditorGUILayout.EnumPopup("打包类型", (MakePackTypeEnum) this.MakePackType).GetHashCode();
            if (select != this.MakePackType)
            {
                MakePackType = select;
                isdirty = true;
            }

            //不同情况下的渲染
            switch ((MakePackTypeEnum) this.MakePackType)
            {
                case MakePackTypeEnum.所有子文件夹:
                {
                    var label = "将该目录下所有子目录,按子目录分别打包AB! \n 如:A文件夹下有 A/a1,A/a2,A/a3三个文件夹, 则会打包为a1、a2、a3 三个AB包,\n 若存在类似A/1.txt文件, 额外会生成AssetBundle - A,包含1.txt";
                    EditorGUILayout.HelpBox(label, MessageType.Info);
                }
                    break;
                case MakePackTypeEnum.通配符匹配:
                {
                    var label = "按匹配通配符的进行打包，其余则按传入路径打包, 暂只支持单条连线传入.";
                    EditorGUILayout.HelpBox(label, MessageType.Info);
                    var match = EditorGUILayout.TextField("匹配路径:", this.MatchPath);
                    if (match != this.MatchPath)
                    {
                        this.MatchPath = match;
                        isdirty = true;
                    }
                }
                    break;
            }

            base.OnInspectorGUI(node, streamManager, inspector, onValueChanged);
            //脏数据
            if (isdirty)
            {
                AssetGraphTools.UpdateNodeGraph(node);
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

            //搜集所有的 asset reference 
            var comingAssetReferenceList = AssetGraphTools.GetComingAssets(incoming);
            if (comingAssetReferenceList.Count == 0)
            {
                return;
            }

            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;


            switch ((MakePackTypeEnum) this.MakePackType)
            {
                case MakePackTypeEnum.所有子文件夹:
                {
                    //子文件夹分别打包
                    SetABBySubfloder(incoming);
                }
                    break;
                case MakePackTypeEnum.通配符匹配:
                {
                    SetABByMatchPath(incoming);
                }
                    break;
            }


            //按buildinfo中的依赖关系输出
            var outMap = new Dictionary<string, List<AssetReference>>();
            var incomingList = AssetGraphTools.GetComingAssets(incoming);
            foreach (var ar in incomingList)
            {
                if (ar.Equals("assets/resource_svn/runtime/battle/char/empty.txt"))
                {
                    Debug.Log("111111111111");
                }
                var ai = this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                if (ai != null)
                {
                    if (!outMap.ContainsKey(ai.ABName))
                    {
                        outMap[ai.ABName] = new List<AssetReference>();
                    }

                    outMap[ai.ABName].Add(ar);
                }
                else
                {
                    Debug.LogError("不存在资产:" + ar.importFrom);
                }
            }

            //输出节点
            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }


        /// <summary>
        /// 将该目录下 所有子文件夹打包
        /// </summary>
        /// <param name="incoming"></param>
        /// <returns></returns>
        private void SetABBySubfloder(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    var rootfloderPath = ag.Key;
                    if (!Directory.Exists(rootfloderPath))
                    {
                        continue;
                    }


                    var rootguid = BuildTools_AssetBundleV2.AssetPathToGUID(rootfloderPath);
                    Debug.Log($"父目录:{rootfloderPath} - {rootguid}");
                    //搜集子目录
                    var subfolders = Directory.GetDirectories(rootfloderPath, "*", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < subfolders.Length; i++)
                    {
                        var subFolder = subfolders[i];
                        subFolder = IPath.ReplaceBackSlash(subFolder); //.Replace("\\", "/");
                        subfolders[i] = subFolder;
                    }


                    //输出目录
                    foreach (var ar in ag.Value)
                    {
                        bool isInSubfolder = false;
                        //判断子目录
                        foreach (var subFolder in subfolders)
                        {
                            //添加目录资产

                            if (ar.importFrom.StartsWith(subFolder + "/", StringComparison.OrdinalIgnoreCase))
                            {
                                var (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(ar.importFrom, subFolder, (BuildAssetInfos.SetABPackLevel) this.SetLevel,
                                    this.Category + " " + (this.selfNodeGUI != null ? this.selfNodeGUI.Name : this.GetHashCode().ToString()), false);
                                // if (!ret)
                                // {
                                //     Debug.LogError($"【颗粒度】设置AB失败 [{subFolder}] - {ar.importFrom} \n {msg}");
                                // }

                                //设置依赖，依赖资产需要特殊处理在当前根目录下的依赖，跳过按文件夹处理
                                if (this.IsIncludeDependAssets)
                                {
                                    var ai = this.BuildingCtx.BuildAssetInfos.GetAssetInfo(ar.importFrom);
                                    if (ai != null)
                                    {
                                        foreach (var depend in ai.DependAssetList)
                                        {
                                            //在不在同级根目录判断
                                            if (!depend.Equals(ar.importFrom, StringComparison.OrdinalIgnoreCase) && !depend.StartsWith(rootfloderPath, StringComparison.OrdinalIgnoreCase))
                                            {
                                                (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(depend, subFolder, (BuildAssetInfos.SetABPackLevel) this.SetLevel,
                                                    this.Category + " " + (this.selfNodeGUI != null ? this.selfNodeGUI.Name : this.GetHashCode().ToString()), false);
                                                // if (!ret)
                                                // {
                                                //     Debug.LogError($"【颗粒度】[depend]设置AB失败 [{subFolder}] - {ar.importFrom} \n {msg}");
                                                // }
                                            }
                                        }
                                    }
                                }

                                //添加到输出分组
                                isInSubfolder = true;
                                break;
                            }
                        }

                        //父目录//剩下全都打进父目录
                        if (!isInSubfolder)
                        {
                            //添加目录资产
                            //设置AB name
                            var (ret, msg) = this.BuildingCtx.BuildAssetInfos.SetABPack(ar.importFrom, rootfloderPath, (BuildAssetInfos.SetABPackLevel) this.SetLevel,
                                this.Category + " " + (this.selfNodeGUI != null ? this.selfNodeGUI.Name : this.GetHashCode().ToString()), this.IsIncludeDependAssets);
                            // if (!ret)
                            // {
                            //     Debug.LogError($"【颗粒度】设置AB失败 [{rootfloderPath}] -{ar.importFrom} \n  {msg}");
                            // }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 通过通配符设置
        /// </summary>
        private void SetABByMatchPath(IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            if (string.IsNullOrEmpty(MatchPath))
            {
                return;
            }

            PerformGraph.AssetGroups ags = incoming.FirstOrDefault();
            var split = this.MatchPath.ToLower().Split(Settings.KEYWORD_WILDCARD);
            var groupingKeywordPrefix = split[0];
            var groupingKeywordPostfix = split[1];
            if (ags != null)
            {
                var item = ags.assetGroups.FirstOrDefault();
               
                foreach (var ar in item .Value)
                {
                    Regex regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
                    var path = ar.importFrom.ToLower();
                    var ret = regex.IsMatch(path);

                    string abname = "";
                    if (ret)
                    {
                        //使用*通配符的路径作为 ab名
                        Match mc  =  regex.Match(path);
                        abname = mc.Groups[0].Value;
                        
                     
                    }
                    else
                    {
                        abname = item.Key;
                    }
                    
                    this.BuildingCtx.BuildAssetInfos.SetABPack(ar.importFrom,abname, (BuildAssetInfos.SetABPackLevel)this.SetLevel,
                        this.Category + " " + (this.selfNodeGUI != null ? this.selfNodeGUI.Name : this.GetHashCode().ToString()), this.IsIncludeDependAssets);
                }
            }
        }
    }
}
