using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BDFramework.Editor.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

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

        private NodeGUI selfNode;

        /// <summary>
        /// 设置Params
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="isUseHash"></param>
        public void SetBuildParams(string outpath, bool isUseHash)
        {
            BuildParams = new BuildAssetBundleParams()
            {
                OutputPath = outpath,
                IsUseHashName = isUseHash,
            };
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

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged)
        {
            this.selfNode = node;
        }

        #endregion

        public override void Prepare(BuildTarget target, NodeData nodeData, IEnumerable<PerformGraph.AssetGroups> incoming, IEnumerable<ConnectionData> connectionsToOutput, PerformGraph.Output outputFunc)
        {
            if (incoming == null)
            {
                return;
            }

            //构建对象
            BuildInfo = new BuildInfo();
            if (BuildParams == null)
            {
                BuildParams = new BuildAssetBundleParams();
            }


            //设置所有节点参数请求,依次传参
            Debug.Log("outpath:" + BuildParams.OutputPath);

            //搜集runtime资源
            var runtimeAssetList = new List<AssetReference>();
            foreach (var incom in incoming)
            {
                //遍历每一条输入的路径
                foreach (var group in incom.assetGroups)
                {
                    runtimeAssetList.AddRange(group.Value);
                }
            }

            //生成所有资源
            AllfileHashMap = new Dictionary<string, string>();
            DependenciesMap = new Dictionary<string, List<string>>();
            this.GenBuildInfo(runtimeAssetList);

            //依赖的资源
            var dependAssetList = new List<AssetReference>();
            foreach (var assetDataItem in BuildInfo.AssetDataMaps)
            {
                //不包含在runtime资源里面
                var ret = runtimeAssetList.FirstOrDefault((ra) => ra.importFrom.Equals(assetDataItem.Key, StringComparison.OrdinalIgnoreCase));
                if (ret == null)
                {
                    var arf = AssetReference.CreateReference(assetDataItem.Key);
                    dependAssetList.Add(arf);
                }
            }


            //输出
            var outMap = new Dictionary<string, List<AssetReference>>
            {
                {nameof(FloderType.Runtime), runtimeAssetList.ToList()}, //传递新容器
                {nameof(FloderType.Depend), dependAssetList.ToList()}
            };


            var output = connectionsToOutput?.FirstOrDefault();
            if (output != null)
            {
                outputFunc(output, outMap);
            }
        }


        #region 初始化所有Assets信息

        /// <summary>
        /// 生成BuildInfo信息
        /// </summary>
        public void GenBuildInfo(List<AssetReference> assets)
        {
            if (BuildInfo == null)
            {
                BuildInfo = new BuildInfo();
            }

            BuildInfo.Time = DateTime.Now.ToShortDateString();
            int id = 0;
            //搜集所有的依赖
            foreach (var mainAsses in assets)
            {
                var dependeAssetsPath = GetDependencies(mainAsses.importFrom);
                //获取依赖 并加入build info
                foreach (var subAssetPath in dependeAssetsPath)
                {
                    var assetData = new BuildInfo.AssetData();
                    assetData.Id = id;
                    assetData.Hash = GetHashFromAssets(subAssetPath);
                    assetData.ABName = subAssetPath;
                    
                    //判断资源类型
                    assetData.Type = (int) AssetBundleEditorToolsV2.GetAssetType(subAssetPath);
                    
                    //获取依赖
                    var dependeAssetList = GetDependencies(subAssetPath);
                    assetData.DependList.AddRange(dependeAssetList);
                    //添加
                    BuildInfo.AssetDataMaps[subAssetPath] = assetData;
                    id++;
                }
            }


            //TODO AB依赖关系纠正
            /// 已知Unity,bug/设计缺陷：
            ///   1.依赖接口，中会携带自己
            ///   2.如若a.png、b.png 依赖 c.atlas，则abc依赖都会是:a.png 、b.png 、 a.atlas
            foreach (var asset in BuildInfo.AssetDataMaps)
            {
                //依赖中不包含自己
                asset.Value.DependList.Remove(asset.Value.ABName);
            }
        }


        #region 依赖关系

        static Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();

        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string[] GetDependencies(string path)
        {
            //全部小写
            //path = path.ToLower();
            List<string> list = null;
            if (!DependenciesMap.TryGetValue(path, out list))
            {
                list = AssetDatabase.GetDependencies(path).Select((s) => s.ToLower()).ToList();
                //检测依赖路径
                CheckAssetsPath(list);
                DependenciesMap[path] = list;
            }

            return list.ToArray();
        }

        /// <summary>
        /// 获取可以打包的资源
        /// </summary>
        /// <param name="allDependObjectPaths"></param>
        static private void CheckAssetsPath(List<string> list)
        {
            if (list.Count == 0)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var path = list[i];

                //文件不存在,或者是个文件夹移除
                if (!File.Exists(path) || Directory.Exists(path))
                {
                    list.RemoveAt(i);
                    continue;
                }

                //判断路径是否为editor依赖
                if (path.Contains("/editor/"))
                {
                    list.RemoveAt(i);
                    continue;
                }

                //特殊后缀
                var ext = Path.GetExtension(path).ToLower();
                if (ext == ".cs" || ext == ".js" || ext == ".dll")
                {
                    list.RemoveAt(i);
                    continue;
                }
            }
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
    }
}
