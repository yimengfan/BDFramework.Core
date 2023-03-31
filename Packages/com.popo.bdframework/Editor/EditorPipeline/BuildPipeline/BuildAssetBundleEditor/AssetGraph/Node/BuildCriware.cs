using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using LitJson;
using ServiceStack.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;
using Debug = UnityEngine.Debug;
using String = System.String;

namespace BDFramework.Editor.AssetGraph.Node
{
    /// <summary>
    /// 这里将criware直接复制到 目标路径
    /// </summary>
    [CustomNode("BDFramework/[Build]打包Criware音频", 102)]
    public class BuildCriware : UnityEngine.AssetGraph.Node
    {
        /// <summary>
        /// 打包AB的上下文工具
        /// </summary>
        public AssetBundleBuildingContext BuildingCtx { get; set; }


        /// <summary>
        /// criware的资源路径
        /// </summary>
        public string CriWareAssetPath = "";
        public override string ActiveStyle
        {
            get { return "node 7 on"; }
        }

        public override string InactiveStyle
        {
            get { return "node 7"; }
        }

        public override string Category
        {
            get { return "打包Criware"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new BuildCriware();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIInspector inspector, Action onValueChanged)
        {
            
            GUILayout.Label("打包Criware(拷贝方案)");
            CriWareAssetPath = EditorGUILayout.TextField("Criware资源路径:", CriWareAssetPath);

            if (GUILayout.Button("打包测试"))
            {
                var ctx = new AssetBundleBuildingContext()
                {
                    BuildParams = new BuildAssetBundleParams()
                    {
                        BuildTarget = BuildTarget.StandaloneLinux64,
                        OutputPath = BApplication.DevOpsPublishAssetsPath,
                    },
                };
                //打包测试
                BuildCriwareAsset(ctx);
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
            this.BuildingCtx = BDFrameworkAssetsEnv.BuildingCtx;
            if (BuildingCtx.BuildParams.IsBuilding)
            {
                EditorUtility.DisplayProgressBar("构建资产", this.Category, 1);
            }
        }




        /// <summary>
        /// 构建时候触发
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="nodeData"></param>
        /// <param name="incoming"></param>
        /// <param name="connectionsToOutput"></param>
        /// <param name="outputFunc"></param>
        /// <param name="progressFunc"></param>
        public override void Build(BuildTarget buildTarget, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc,
            Action<NodeData, string, float> progressFunc)
        {
           
            BuildCriwareAsset(this.BuildingCtx);
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 打包criware资产
        /// </summary>
        public void BuildCriwareAsset(AssetBundleBuildingContext ctx)
        {
            var sourcePath = IPath.Combine(CriWareAssetPath, BApplication.GetPlatformPath(ctx.BuildParams.BuildTarget));
            if (!Directory.Exists(sourcePath))
            {
                sourcePath = CriWareAssetPath;
            }
            //
            var targetPath = IPath.Combine(this.BuildingCtx.BuildParams.OutputPath, BApplication.GetPlatformPath(ctx.BuildParams.BuildTarget),BResources.SOUND_ASSET_PATH);
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath,true);
            }
            //开始拷贝
            FileHelper.CopyFolderTo(sourcePath,targetPath);
            //删除meta
            var metas = Directory.GetFiles(targetPath, "*.meta", SearchOption.AllDirectories);
            foreach (var meta in metas)
            {
                File.Delete(meta);
            }
            
            //criware
            var criWare =  Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories);
            BDebug.Log($"打包Criware成功！数量: {criWare.Length} ,source:{sourcePath} target:{targetPath}", Color.green);
        }
        
        
    }
}
