using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
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
        /// Build AB包的信息
        /// </summary>
        static public BuildInfo BuildInfo { get; set; }

        /// <summary>
        /// 打包Assetbundle的参数
        /// </summary>
        static public BuildAssetBundleParams BuildParams { get; set; }

        /// <summary>
        /// 设置Params
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="isUseHash"></param>
        public void SetBuildParams(string outpath)
        {
            BuildParams = new BuildAssetBundleParams() {OutputPath = outpath};
            this.isGenBuildInfo = false;
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
            data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new BDFrameworkAssetsEnv();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            //this.selfNode = node;
        }

        #endregion

        private bool isGenBuildInfo = false;

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            StopwatchTools.Begin();
            //构建打包参数
            if (BuildParams == null)
            {
                BuildParams = new BuildAssetBundleParams();
                
            }
            BuildParams.BuildTarget = target;

            //设置所有节点参数请求,依次传参
            Debug.Log("【初始化框架资源环境】配置:\n" + JsonMapper.ToJson(BuildParams));
            //搜集runtime资源
            var allRuntimeAssetList = this.LoadAllRuntimeAssets();
            //生成buildinfo
            if (!isGenBuildInfo) //防止GUI每次调用prepare时候都触发,真正打包时候 会重新构建
            {
                Debug.Log("------------>生成BuildInfo");
                BuildInfo = this.GenBuildInfo(target, allRuntimeAssetList);
                isGenBuildInfo = true;
            }

            //生成所有资源
            AllfileHashMap = new Dictionary<string, string>();
            DependenciesMap = new Dictionary<string, List<string>>();

            //依赖的资源
            var dependAssetList = new List<AssetReference>();
            var runtimeDirects = BDApplication.GetAllRuntimeDirects();
            foreach (var assetDataItem in BuildInfo.AssetDataMaps)
            {
                var assetdata = assetDataItem.Value;
                //不包含在runtime资源里面
                var ret = allRuntimeAssetList.Find((ra) => ra.importFrom.Equals(assetDataItem.Key, StringComparison.OrdinalIgnoreCase));
                if (ret == null)
                {
                    var arf = AssetReference.CreateReference(assetDataItem.Key);
                    dependAssetList.Add(arf);
                }
            }

            StopwatchTools.End("【初始化框架资源环境】");
            //
            var count = allRuntimeAssetList.Count + dependAssetList.Count;
            if (BuildInfo.AssetDataMaps.Count != count)
            {
                Debug.LogErrorFormat("【初始化框架资源环境】buildinfo:{0} output:{1}", BuildInfo.AssetDataMaps.Count, count);
                var map = JsonMapper.ToObject<Dictionary<string, BuildInfo.BuildAssetData>>(JsonMapper.ToJson(BuildInfo.AssetDataMaps));
                foreach (var ra in allRuntimeAssetList)
                {
                    map.Remove(ra.importFrom);
                }

                foreach (var drf in dependAssetList)
                {
                    map.Remove(drf.importFrom);
                }

                Debug.Log(JsonMapper.ToJson(map, true));
            }

            //输出
            var outMap = new Dictionary<string, List<AssetReference>>
            {
                {nameof(FloderType.Runtime), allRuntimeAssetList.ToList()}, //传递新容器
                {nameof(FloderType.Depend), dependAssetList.ToList()}
            };

            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
                // outputFunc(output, new Dictionary<string, List<AssetReference>>());
            }
        }


        public override void Build(BuildTarget buildTarget, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc,
            Action<NodeData, string, float> progressFunc)
        {
            #region 保存AssetTypeConfig
            var asetTypePath = string.Format("{0}/{1}/{2}", BuildParams.OutputPath, BDApplication.GetPlatformPath(buildTarget), BResources.ASSET_TYPES_PATH);
            //数据结构保存
            AssetTypes at = new AssetTypes()
            {
                AssetTypeList = AssetTypeList,
            };
            var csv = CsvSerializer.SerializeToString(at);
            FileHelper.WriteAllText(asetTypePath, csv);
            Debug.LogFormat("AssetType写入到:{0} \n{1}", asetTypePath, csv);
            
            #endregion
            
            //BD生命周期触发
            BDFrameworkPublishPipelineHelper.OnBeginBuildAssetBundle(BuildParams,BuildInfo);
        }


        #region 加载Runtime目录

        /// <summary>
        /// 加载runtime的Asset信息
        /// </summary>
        /// <returns></returns>
        public List<AssetReference> LoadAllRuntimeAssets()
        {
            var allRuntimeDirects = BDApplication.GetAllRuntimeDirects();
            var assetPathList = new List<string>();
            var assetList = new List<AssetReference>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var runtimePath in allRuntimeDirects)
            {
                //创建
                var runtimeGuids = AssetDatabase.FindAssets("", new string[] {runtimePath});
                assetPathList.AddRange(runtimeGuids);
            }

            //去重
            assetPathList = assetPathList.Distinct().Select((guid) => AssetDatabase.GUIDToAssetPath(guid)).ToList();
            assetPathList = CheckAssetsPath(assetPathList);
            //
            foreach (var path in assetPathList)
            {
                //var path = AssetDatabase.GUIDToAssetPath(guid);
                //无法获取类型资源，移除
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null)
                {
                    Debug.LogError("【Loder】无法获取资源类型:" + path);
                    continue;
                }

                var outAR = AssetReference.CreateReference(path);
                //添加输出
                assetList.Add(outAR);
            }

            Debug.LogFormat("LoadAllRuntimeAssets耗时:{0}ms", sw.ElapsedMilliseconds);
            return assetList;
        }

        #endregion


        #region 初始化所有Assets信息

        //资源类型列表
        List<string> AssetTypeList = new List<string>();

        /// <summary>
        /// 生成BuildInfo信息
        /// </summary>
        public BuildInfo GenBuildInfo(BuildTarget target, List<AssetReference> runtimeAssetList)
        {
            AssetTypeList = new List<string>();

            var sw = new Stopwatch();
            sw.Start();
            var buildInfo = new BuildInfo();
            buildInfo.Time = DateTime.Now.ToShortDateString();
            int id = 0;

            //搜集所有的依赖
            foreach (var mainAsset in runtimeAssetList)
            {
                //这里会包含主资源
                var dependAssetPathList = GetDependAssetList(mainAsset.importFrom);


                //获取依赖信息 并加入buildinfo
                foreach (var dependPath in dependAssetPathList)
                {
                    //防止重复
                    if (buildInfo.AssetDataMaps.ContainsKey(dependPath))
                    {
                        continue;
                    }

                    //判断资源类型
                    var type = AssetBundleEditorToolsV2.GetMainAssetTypeAtPath(dependPath);
                    if (type == null)
                    {
                        Debug.LogError("获取资源类型失败:" + dependPath);
                        continue;
                    }

                    //构建资源类型
                    var assetData = new BuildInfo.BuildAssetData();
                    assetData.Id = id;
                    assetData.Hash = GetHashFromAssets(dependPath);
                    assetData.ABName = dependPath;
                    var idx = AssetTypeList.FindIndex((a) => a == type.FullName);
                    if (idx == -1)
                    {
                        AssetTypeList.Add(type.FullName);
                        idx = AssetTypeList.Count - 1;
                    }

                    assetData.Type = idx;
                    //获取依赖
                    var dependeAssetList = GetDependAssetList(dependPath);
                    assetData.DependAssetList.AddRange(dependeAssetList);
                    //添加
                    buildInfo.AssetDataMaps[dependPath] = assetData;
                    id++;
                }
            }


            //TODO AB依赖关系纠正
            /// 已知Unity,bug/设计缺陷：
            ///   1.依赖接口，中会携带自己
            ///   2.如若a.png、b.png 依赖 c.atlas，则abc依赖都会是:a.png 、b.png 、 a.atlas
            foreach (var asset in buildInfo.AssetDataMaps)
            {
                //依赖中不包含自己
                asset.Value.DependAssetList.Remove(asset.Value.ABName);
            }

            //检查
            foreach (var ar in runtimeAssetList)
            {
                if (!buildInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                {
                    Debug.LogError("AssetDataMaps遗漏资源:" + ar.importFrom);
                }
            }

            Debug.LogFormat("【GenBuildInfo】耗时:{0}ms.", sw.ElapsedMilliseconds);
            return buildInfo;
        }


        #region 依赖关系

        static Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();

        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string[] GetDependAssetList(string path)
        {
            //全部小写
            //path = path.ToLower();
            List<string> dependList = null;
            if (!DependenciesMap.TryGetValue(path, out dependList))
            {
                dependList = AssetDatabase.GetDependencies(path).Select((s) => s.ToLower()).ToList();

                //检测依赖路径
                dependList = CheckAssetsPath(dependList);


                DependenciesMap[path] = dependList;
            }

            return dependList.ToArray();
        }

        /// <summary>
        /// 获取可以打包的资源
        /// </summary>
        /// <param name="allDependObjectPaths"></param>
        static private List<string> CheckAssetsPath(List<string> assetPathList)
        {
            if (assetPathList.Count == 0)
            {
                return assetPathList;
            }

            for (int i = assetPathList.Count - 1; i >= 0; i--)
            {
                var path = assetPathList[i];

                // //文件夹移除
                // if (AssetDatabase.IsValidFolder(path))
                // {
                //     Debug.Log("【依赖验证】移除目录资产" + path);
                //     assetPathList.RemoveAt(i);
                //     continue;
                // }

                //特殊后缀
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    assetPathList.RemoveAt(i);
                    continue;
                }

                //文件不存在,或者是个文件夹移除
                if (!File.Exists(path) || Directory.Exists(path))
                {
                    assetPathList.RemoveAt(i);
                    continue;
                }

                //判断路径是否为editor依赖
                if (path.Contains("/editor/", StringComparison.OrdinalIgnoreCase) //一般的编辑器资源
                    || path.Contains("/Editor Resources/", StringComparison.OrdinalIgnoreCase) //text mesh pro的编辑器资源
                )
                {
                    assetPathList.RemoveAt(i);
                    Debug.LogWarning("【依赖验证】移除Editor资源" + path);
                    continue;
                }
            }

            return assetPathList;
        }

        #endregion

        #region 文件 md5

        private static Dictionary<string, string> AllfileHashMap { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 获取文件的md5
        /// 同时用资产+资产meta 取 sha256
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string GetHashFromAssets(string fileName)
        {
            var str = "";
            if (AllfileHashMap.TryGetValue(fileName, out str))
            {
                return str;
            }

            try
            {
                //这里使用 asset + meta 生成hash,防止其中一个修改导致的文件变动 没更新
                var assetBytes = File.ReadAllBytes(fileName);
                var metaBytes = File.ReadAllBytes(fileName + ".meta");
                List<byte> byteList = new List<byte>();
                byteList.AddRange(assetBytes);
                byteList.AddRange(metaBytes);
                //这里为了防止碰撞 考虑Sha256 512 但是速度会更慢
                var sha1 = SHA256.Create();
                byte[] retVal = sha1.ComputeHash(byteList.ToArray());
                //hash
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                var hash = sb.ToString();
                AllfileHashMap[fileName] = hash;
                return hash;
            }
            catch (Exception ex)
            {
                Debug.LogError("hash计算错误:" + fileName);
                return "";
            }
        }

        #endregion

        #endregion

        //   override 

        /// <summary>
        /// 刷新节点值
        /// </summary>
        /// <param name="nodeGUI"></param>
        static public void UpdateNodeGraph(NodeGUI nodeGUI)
        {
            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_UPDATED, nodeGUI));
        }

        /// <summary>
        /// 更新连接线
        /// </summary>
        /// <param name="nodeGUI"></param>
        /// <param name="outputConnect"></param>
        static public void UpdateConnectLine(NodeGUI nodeGUI, ConnectionPointData outputConnect)
        {
            if (outputConnect == null)
            {
                return;
            }


            NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, nodeGUI, Vector2.zero, outputConnect));
        }
    }
}
