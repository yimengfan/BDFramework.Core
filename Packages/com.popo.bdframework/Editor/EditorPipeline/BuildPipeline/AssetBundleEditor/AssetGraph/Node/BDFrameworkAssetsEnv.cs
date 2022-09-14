using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.Editor.Tools;
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

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/[*]初始化框架Assets环境", 1)]
    public class BDFrameworkAssetsEnv : UnityEngine.AssetGraph.Node
    {
        public enum FloderType
        {
            Runtime,
            Depend, //runtime依赖的目录
            SpriteAtlas, //图集
            Shaders,
        }


        /// <summary>
        /// 打包AB的上下文工具
        /// </summary>
        static public AssetBundleBuildingContext BuildingCtx { get; set; }

        /// <summary>
        /// 重置
        /// </summary>
        static public void Reset()
        {
            BuildingCtx = null;
        }

        /// <summary>
        /// 设置Params
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="isUseHash"></param>
        public void SetBuildParams(string outpath, bool isBuilding)
        {
            BuildingCtx = new AssetBundleBuildingContext();
            BuildingCtx.BuildParams.OutputPath = outpath;
            BuildingCtx.BuildParams.IsBuilding = isBuilding;
        }


        #region 渲染相关信息

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
            get { return "初始化框架Assets环境"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new BDFrameworkAssetsEnv();
        }


        /// <summary>
        /// 文件夹AB规则
        /// </summary>
        public enum AssetGraphWindowsModeEnum
        {
            /// <summary>
            /// 设置AB名为父文件夹路径
            /// </summary>
            配置节点模式,

            /// <summary>
            /// 设置所有子文件夹中的文件，AB名为子文件夹名
            /// </summary>
            预览节点模式
        }

        /// <summary>
        /// 设置规则
        /// 这里的值一定要public，不然sg 用json序列化判断值未变化，则不会刷新
        /// </summary>
        public int AssetGraphWindowsMode = (int) AssetGraphWindowsModeEnum.配置节点模式;

        /// <summary>
        /// 是否为编辑模式
        /// </summary>
        private bool IsEditMode
        {
            get { return AssetGraphWindowsMode == (int) AssetGraphWindowsModeEnum.配置节点模式; }
        }

        /// <summary>
        /// 节点缓存
        /// </summary>
        private NodeGUI selfNodeGUI;

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            GUILayout.Label("初始化打包框架", EditorGUIHelper.LabelH4);
            this.selfNodeGUI = node;
            bool isDirty = false;

            //渲染数据.
            //包装一层 方便监听改动
            var ret = EditorGUILayout.EnumPopup("窗口模式", (AssetGraphWindowsModeEnum) this.AssetGraphWindowsMode).GetHashCode();
            if (ret != this.AssetGraphWindowsMode)
            {
                this.AssetGraphWindowsMode = ret;
                isDirty = true;
            }

            //根据不同的枚举进行提示
            switch ((AssetGraphWindowsModeEnum) this.AssetGraphWindowsMode)
            {
                case AssetGraphWindowsModeEnum.配置节点模式:
                {
                    EditorGUILayout.HelpBox("该模式下,不会加载实际Assets数据,避免卡顿!", MessageType.Info);
                }
                    break;

                case AssetGraphWindowsModeEnum.预览节点模式:
                {
                    EditorGUILayout.HelpBox("该模式下,所有操作都会预览实际Assets打包情况，比较卡!", MessageType.Info);
                }
                    break;
            }

            if (GUILayout.Button("强制刷新资源数据"))
            {
                isDirty = true;
                GenBuildingCtx(this.loadRuntimeAssetPathList, true);
            }


            if (isDirty)
            {
                Debug.Log("更新node!");
                //触发
                //BDFrameworkAssetsEnv.UpdateConnectLine(this.selfNodeGUI, this.selfNodeGUI.Data.OutputPoints.FirstOrDefault());
                AssetGraphTools.UpdateNodeGraph(this.selfNodeGUI);
            }
        }

        #endregion

        private List<string> loadRuntimeAssetPathList = new List<string>();

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

            //检测混淆
            AssetGraphTools.WatchBegin();
            if (BuildingCtx == null)
            {
                BuildingCtx = new AssetBundleBuildingContext();
            }

            BuildingCtx.BuildParams.BuildTarget = target;

            //拿到外部传入的加载目录
            loadRuntimeAssetPathList.Clear();
            foreach (var ags in incoming)
            {
                foreach (var ag in ags.assetGroups)
                {
                    loadRuntimeAssetPathList.Add(ag.Key);
                }
            }

            Debug.LogError("传入路径:\n" + JsonMapper.ToJson(loadRuntimeAssetPathList, true));


            //设置所有节点参数请求,依次传参
            Debug.Log("【初始化框架资源环境】配置:\n" + JsonMapper.ToJson(BuildingCtx.BuildParams));
            var outMap = new Dictionary<string, List<AssetReference>>();
            //预览模式
            if ((AssetGraphWindowsModeEnum) this.AssetGraphWindowsMode == AssetGraphWindowsModeEnum.预览节点模式 || BuildingCtx.BuildParams.IsBuilding)
            {
                //创建构建上下文信息
                GenBuildingCtx(this.loadRuntimeAssetPathList);

                //输出
                outMap = new Dictionary<string, List<AssetReference>>()
                {
                    {nameof(FloderType.Runtime), BuildingCtx.RuntimeAssetsList.ToList()}, //传递新容器
                    {nameof(FloderType.Depend), BuildingCtx.DependAssetList.ToList()}
                };
            }
            else if ((AssetGraphWindowsModeEnum) this.AssetGraphWindowsMode == AssetGraphWindowsModeEnum.配置节点模式)
            {
                //输出
                outMap = new Dictionary<string, List<AssetReference>>()
                {
                    {nameof(FloderType.Runtime), new List<AssetReference>()}, //传递新容器
                    {nameof(FloderType.Depend), new List<AssetReference>()}
                };
            }

            AssetGraphTools.WatchEnd("【初始化框架资源环境】");

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
                // 
            }
        }

        /// <summary>
        /// 生成BuildingCtx
        /// </summary>
        private void GenBuildingCtx(List<string> assetDirectories, bool isRenew = false)
        {
            //新构建对象
            if (isRenew)
            {
                string lastouput = BuildingCtx.BuildParams.OutputPath;
                BuildingCtx = new AssetBundleBuildingContext();
                BuildingCtx.BuildParams.OutputPath = lastouput;
            }

            //生成build资源信息
            if (BuildingCtx.BuildAssetInfos.GetAssetsCount() == 0)
            {
                BuildingCtx.CollectBuildAssets(assetDirectories.ToArray());
            }
        }


        public override void Build(BuildTarget buildTarget, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc,
            Action<NodeData, string, float> progressFunc)
        {
            #region 保存AssetTypeConfig

            var asetTypePath = string.Format("{0}/{1}/{2}", BuildingCtx.BuildParams.OutputPath, BApplication.GetPlatformPath(buildTarget), BResources.ART_ASSET_TYPES_PATH);
            //数据结构保存
            AssetTypeConfig at = new AssetTypeConfig()
            {
                AssetTypeList = BuildingCtx.BuildAssetInfos.AssetTypeList,
            };
            var csv = CsvSerializer.SerializeToString(at);
            FileHelper.WriteAllText(asetTypePath, csv);
            Debug.LogFormat("AssetType写入到:{0} \n{1}", asetTypePath, csv);

            #endregion

            //BD生命周期触发
            BDFrameworkPipelineHelper.OnBeginBuildAssetBundle(BuildingCtx);
        }
    }
}
