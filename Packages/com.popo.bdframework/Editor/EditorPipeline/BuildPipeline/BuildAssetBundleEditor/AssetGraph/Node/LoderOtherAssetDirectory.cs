using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[Loder]附加打包资产路径(避免使用)", 1)]
    public class LoderOtherAssetDirectory : UnityEngine.AssetGraph.Node
    {
        //加载路径
        public string LoadAssetPath = "";

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
            get { return "[Loder]附加打包资产路径(避免使用)"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new LoderOtherAssetDirectory();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            EditorGUILayout.HelpBox("支持* ?通配符", MessageType.Info);
            var path = EditorGUILayout.TextField("打包路径:", this.LoadAssetPath);
            node.Name = "Load: " + path;


            //改变触发变动
            if (path != this.LoadAssetPath)
            {
                this.LoadAssetPath = IPath.ReplaceBackSlash(path);
                AssetGraphTools.UpdateNodeGraph(node);
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
            //输出传入的
            var outMap = new Dictionary<string, List<AssetReference>>();


            if (this.LoadAssetPath.Contains("*"))
            {
                var split = this.LoadAssetPath.ToLower().Split(Settings.KEYWORD_WILDCARD);
                var root = split[0];
                var searchPattern = split[1];
                Regex regex = new Regex(root + "(.*?)" + searchPattern);
                var dirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
                //
                foreach (var dir in dirs)
                {
                    var _dir = IPath.ReplaceBackSlash(dir);
                    var match = regex.IsMatch(_dir.ToLower());
                    if (match)
                    {
                        outMap[_dir] = new List<AssetReference>();
                    }
                }
            }
            else
            {
                //这里拿key当参数传递
                if (!Directory.Exists(this.LoadAssetPath))
                {
                    Debug.LogError("不存在路径:" + this.LoadAssetPath);
                }

                outMap[this.LoadAssetPath] = new List<AssetReference>();
            }

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }
    }
}
