using BDFramework.Core.Tools;
using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using Debug = UnityEngine.Debug;


namespace BDFramework.Editor.AssetBundle
{
    public class AssetBundleBuildToolsV2
    {
        static public string RUNTIME_PATH = "/runtime/";

        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="outputPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isUseHashName">是否为hash name</param>
        public static bool GenAssetBundle(string outputPath, RuntimePlatform platform)
        {
            var buildTarget = BDApplication.GetBuildTarget(platform);
            AssetBundleEditorToolsV2ForAssetGraph.Build(buildTarget, outputPath);
            return true;
        }

        /// <summary>
        /// 获取主资源类型
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Type GetMainAssetTypeAtPath(string path)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            //图片类型得特殊判断具体的实例类型
            if (type == typeof(Texture2D))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null)
                {
                    return typeof(Sprite);
                }

                var tex2d = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex2d != null)
                {
                    return typeof(Texture2D);
                }

                var tex3d = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
                if (tex3d != null)
                {
                    return typeof(Texture3D);
                }
            }

            return type;
        }

        //生成所有资源
        Dictionary<string, string> fileHashCacheMap = new Dictionary<string, string>();

        /// <summary>
        /// 依赖列表
        /// </summary>
        Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();

        /// <summary>
        /// 构建资产的数据
        /// </summary>
        public BuildAssetsInfo BuildAssetsInfo { get; private set; } = new BuildAssetsInfo();

        /// <summary>
        /// 所有资产的类型
        /// </summary>
        public List<string> AssetTypeList { get; private set; } = new List<string>();

        /// <summary>
        /// runtime下的资源
        /// </summary>
        public List<AssetReference> RuntimeAssetsList { get; private set; } = new List<AssetReference>();

        /// <summary>
        /// Runtime的依赖资源
        /// </summary>
        public List<AssetReference> DependAssetList { get; private set; } = new List<AssetReference>();

        #region 获取可打包资源信息

        /// <summary>
        /// 生成BuildInfo信息
        /// </summary>
        public bool GenBuildInfo()
        {
            //初始化数据
            this.AssetTypeList = new List<string>();
            this.BuildAssetsInfo = new BuildAssetsInfo();
            this.RuntimeAssetsList = GetRuntimeAssetsInfo();
          
            //
            var sw = new Stopwatch();
            sw.Start();

            BuildAssetsInfo.Time = DateTime.Now.ToShortDateString();
            int id = 0;

            //搜集所有的依赖
            foreach (var mainAsset in this.RuntimeAssetsList)
            {
                //这里会包含主资源
                var dependAssetPathList = GetDependAssetList(mainAsset.importFrom);

                //获取依赖信息 并加入buildinfo
                foreach (var dependPath in dependAssetPathList)
                {
                    //防止重复
                    if (BuildAssetsInfo.AssetDataMaps.ContainsKey(dependPath))
                    {
                        continue;
                    }

                    //判断资源类型
                    var type = GetMainAssetTypeAtPath(dependPath);
                    if (type == null)
                    {
                        Debug.LogError("获取资源类型失败:" + dependPath);
                        continue;
                    }

                    //构建资源类型
                    var assetData = new BuildAssetsInfo.BuildAssetData();
                    assetData.Id = id;
                    assetData.Hash = this.GetHashFromAssets(dependPath);
                    assetData.ABName = dependPath;
                    var idx = AssetTypeList.FindIndex((a) => a == type.FullName);
                    if (idx == -1)
                    {
                        AssetTypeList.Add(type.FullName);
                        idx = AssetTypeList.Count - 1;
                    }

                    assetData.Type = idx;
                    //获取依赖
                    var dependeAssetList = this.GetDependAssetList(dependPath);
                    assetData.DependAssetList.AddRange(dependeAssetList);
                    //添加
                    BuildAssetsInfo.AssetDataMaps[dependPath] = assetData;
                    id++;
                }
            }

            //TODO AB依赖关系纠正
            /// 已知Unity,bug/设计缺陷：
            ///   1.依赖接口，中会携带自己
            ///   2.如若a.png、b.png 依赖 c.atlas，则abc依赖都会是:a.png 、b.png 、 a.atlas
            foreach (var asset in BuildAssetsInfo.AssetDataMaps)
            {
                //依赖中不包含自己
                asset.Value.DependAssetList.Remove(asset.Value.ABName);
            }
            
            
            //获取依赖
            this.DependAssetList = this.GetDependAssetsinfo();
           //---------------------------------------end---------------------------------------------------------
            
           //检查
            foreach (var ar in this.RuntimeAssetsList)
            {
                if (!BuildAssetsInfo.AssetDataMaps.ContainsKey(ar.importFrom))
                {
                    Debug.LogError("AssetDataMaps遗漏资源:" + ar.importFrom);
                }
            }
            Debug.LogFormat("【GenBuildInfo】耗时:{0}ms.", sw.ElapsedMilliseconds);
            //检测构造的数据
            var count = this.RuntimeAssetsList.Count + this.DependAssetList.Count;
            if (BuildAssetsInfo.AssetDataMaps.Count != count)
            {
                Debug.LogErrorFormat("【初始化框架资源环境】出错! buildinfo:{0} output:{1}", BuildAssetsInfo.AssetDataMaps.Count, count);

                var tmpBuildAssetsInfo = BuildAssetsInfo.Clone();
                foreach (var ra in this.RuntimeAssetsList)
                {
                    tmpBuildAssetsInfo.AssetDataMaps.Remove(ra.importFrom);
                }

                foreach (var drf in this.DependAssetList)
                {
                    tmpBuildAssetsInfo.AssetDataMaps.Remove(drf.importFrom);
                }

                Debug.Log(JsonMapper.ToJson(tmpBuildAssetsInfo.AssetDataMaps, true));

                return false;
            }

            return true;
        }


        /// <summary>
        /// 加载runtime的Asset信息
        /// </summary>
        /// <returns></returns>
        public List<AssetReference> GetRuntimeAssetsInfo()
        {
            var allRuntimeDirects = BDApplication.GetAllRuntimeDirects();
            var assetPathList = new List<string>();
            var retAssetList = new List<AssetReference>();
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
            assetPathList = AssetBundleBuildToolsV2.CheckAssetsPath(assetPathList.ToArray());
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
                retAssetList.Add(outAR);
            }

            Debug.LogFormat("LoadAllRuntimeAssets耗时:{0}ms", sw.ElapsedMilliseconds);
            return retAssetList;
        }

        /// <summary>
        /// 获取依赖资源的info
        /// </summary>
        /// <returns></returns>
        public List<AssetReference> GetDependAssetsinfo()
        {
            //依赖的资源
            var dependAssetList = new List<AssetReference>();
            foreach (var assetDataItem in BuildAssetsInfo.AssetDataMaps)
            {
                //不包含在runtime资源里面
                var ret = this.RuntimeAssetsList.Find((ra) => ra.importFrom.Equals(assetDataItem.Key, StringComparison.OrdinalIgnoreCase));
                if (ret == null)
                {
                    var arf = AssetReference.CreateReference(assetDataItem.Key);
                    dependAssetList.Add(arf);
                }
            }

            return dependAssetList;
        }

        #endregion


        #region 打包

        #endregion


        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string[] GetDependAssetList(string path)
        {
            //全部小写
            //path = path.ToLower();
            List<string> dependList = null;
            if (!DependenciesMap.TryGetValue(path, out dependList))
            {
                dependList = AssetDatabase.GetDependencies(path).Select((s) => s.ToLower()).ToList();
                //检测依赖路径
                dependList = CheckAssetsPath(dependList.ToArray());
                DependenciesMap[path] = dependList;
            }

            return dependList.ToArray();
        }


        /// <summary>
        /// 资源验证
        /// </summary>
        /// <param name="allDependObjectPaths"></param>
        static public List<string> CheckAssetsPath(params string[] assetPathArray)
        {
            var retList = new List<string>(assetPathArray);

            for (int i = assetPathArray.Length - 1; i >= 0; i--)
            {
                var path = assetPathArray[i];

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
                    retList.RemoveAt(i);
                    continue;
                }

                //文件不存在,或者是个文件夹移除
                if (!File.Exists(path) || Directory.Exists(path))
                {
                    retList.RemoveAt(i);
                    continue;
                }

                //判断路径是否为editor依赖
                if (path.Contains("/editor/", StringComparison.OrdinalIgnoreCase) //一般的编辑器资源
                    || path.Contains("/Editor Resources/", StringComparison.OrdinalIgnoreCase) //text mesh pro的编辑器资源
                   )
                {
                    retList.RemoveAt(i);
                    Debug.LogWarning("【依赖验证】移除Editor资源" + path);
                    continue;
                }
            }

            return retList;
        }


        /// <summary>
        /// 获取文件的md5
        /// 同时用资产+资产meta 取 sha256
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetHashFromAssets(string fileName)
        {
            var str = "";
            if (fileHashCacheMap.TryGetValue(fileName, out str))
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
                var hash = FileHelper.GetMurmurHash3(byteList.ToArray());
                fileHashCacheMap[fileName] = hash;
                return hash;
            }
            catch (Exception ex)
            {
                Debug.LogError("hash计算错误:" + fileName);
                return "";
            }
        }
    }
}
