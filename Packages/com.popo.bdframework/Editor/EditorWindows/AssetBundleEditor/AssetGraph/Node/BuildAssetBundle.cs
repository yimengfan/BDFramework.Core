using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetBundle;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using LitJson;
using UnityEditor;
using UnityEngine;
using UnityEngine.AssetGraph;
using UnityEngine.AssetGraph.DataModel.Version2;

namespace BDFramework.Editor.AssetGraph.Node
{
    [CustomNode("BDFramework/打包AssetBundle", 100)]
    public class BuildAssetBundle : UnityEngine.AssetGraph.Node
    {
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
            get { return "打包AssetBundle"; }
        }

        public override void Initialize(NodeData data)
        {
            data.AddDefaultInputPoint();
            //data.AddDefaultOutputPoint();
        }

        public override UnityEngine.AssetGraph.Node Clone(NodeData newData)
        {
            return new BuildAssetBundle();
        }

        public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager,
            NodeGUIEditor editor,
            Action onValueChanged)
        {
        }


        /// <summary>
        /// 编译
        /// </summary>
        public void Build()
        {
            var buildInfo = BDFrameworkAssetsEnv.BuildInfo;
            var buildParams = BDFrameworkAssetsEnv.BuildAssetBundleParams;
            this.MergeABName(buildInfo, buildParams);
            this.GenArtConfig(buildInfo, buildParams);
            this.BuildAB(buildInfo, buildParams);
        }

        static string RUNTIME_PATH = "/runtime/";

        /// <summary>
        /// 合并ABname
        /// </summary>
        private void MergeABName(BuildInfo buildInfo, BuildAssetBundleParams buildParams)
        {
            #region 整理依赖关系

            //1.把依赖资源替换成AB Name，
            foreach (var assetItem in buildInfo.AssetDataMaps.Values)
            {
                for (int i = 0; i < assetItem.DependList.Count; i++)
                {
                    var dependAsset = assetItem.DependList[i];
                    var dependAssetData = buildInfo.AssetDataMaps[dependAsset];
                    //替换成真正AB名
                    if (!string.IsNullOrEmpty(dependAssetData.ABName))
                    {
                        assetItem.DependList[i] = dependAssetData.ABName;
                    }
                }

                //去重
                assetItem.DependList = assetItem.DependList.Distinct().ToList();
                assetItem.DependList.Remove(assetItem.ABName);
            }


            if (buildParams.IsUseHashName)
            {
                //使用guid 作为ab名
                foreach (var asset in buildInfo.AssetDataMaps)
                {
                    var abname = AssetDatabase.AssetPathToGUID(asset.Value.ABName);
                    if (!string.IsNullOrEmpty(abname)) //不存在的资源（如ab.shader之类）,则用原名
                    {
                        asset.Value.ABName = abname;
                    }
                    else
                    {
                        Debug.LogError("获取GUID失败：" + asset.Value.ABName);
                    }

                    for (int i = 0; i < asset.Value.DependList.Count; i++)
                    {
                        var dependAssetName = asset.Value.DependList[i];

                        abname = AssetDatabase.AssetPathToGUID(dependAssetName);
                        if (!string.IsNullOrEmpty(abname)) //不存在的资源（如ab.shader之类）,则用原名
                        {
                            asset.Value.DependList[i] = abname;
                        }
                        else
                        {
                            Debug.LogError("获取GUID失败：" + dependAssetName);
                        }
                    }
                }
            }
            else
            {
                //2.整理runtime路径 替换路径名为Resource规则的名字
                // 非Hash命名时，runtime目录的都放在一起，方便调试
                foreach (var assetData in buildInfo.AssetDataMaps)
                {
                    if (assetData.Key.Contains(RUNTIME_PATH))
                    {
                        var newName = assetData.Value.ABName;
                        //移除runtime之前的路径、后缀
                        var index = newName.IndexOf(RUNTIME_PATH);
                        newName = newName.Substring(index + 1); //runtimeStr.Length);

                        var extension = Path.GetExtension(newName);
                        if (!string.IsNullOrEmpty(extension))
                        {
                            newName = newName.Replace(extension, "");
                        }

                        buildInfo.SetABName(assetData.Key, newName);
                    }
                }
            }

            #endregion
        }


        /// <summary>
        ///生成Runtime下的Art.Config
        /// </summary>
        private void GenArtConfig(BuildInfo buildInfo, BuildAssetBundleParams buildParams)
        {
            //根据buildinfo 生成加载用的 Config
            //1.只保留Runtime目录下的配置
            ManifestConfig config = new ManifestConfig();
            config.IsHashName = buildParams.IsUseHashName;
            //
            foreach (var item in buildInfo.AssetDataMaps)
            {
                //runtime路径下，
                //改成用Resources加载规则命名的key
                if (item.Key.Contains(RUNTIME_PATH))
                {
                    var key = item.Key;
                    //移除runtime之前的路径、后缀
                    var index = key.IndexOf(RUNTIME_PATH);
                    if (config.IsHashName)
                    {
                        key = key.Substring(index + RUNTIME_PATH.Length); //hash要去掉runtime
                    }
                    else
                    {
                        key = key.Substring(index + 1); // 保留runtime
                    }

                    var exten = Path.GetExtension(key);
                    if (!string.IsNullOrEmpty(exten))
                    {
                        key = key.Replace(exten, "");
                    }

                    //添加manifest
                    var mi = new ManifestItem(item.Value.ABName, (ManifestItem.AssetTypeEnum) item.Value.Type, new List<string>(item.Value.DependList));
                    config.ManifestMap[key] = mi;
                }
            }


            //写入
            var outputPath = Path.Combine(buildParams.OutputPath, BDApplication.GetPlatformPath(buildParams.Platform));
            var configPath = IPath.Combine(outputPath, BResources.ART_CONFIG_PATH);
            FileHelper.WriteAllText(configPath, JsonMapper.ToJson(config));
        }


        /// <summary>
        /// 打包Asset Bundle
        /// </summary>
        /// <param name="buildInfo"></param>
        /// <param name="buildParams"></param>
        private void BuildAB(BuildInfo buildInfo, BuildAssetBundleParams buildParams)
        {
            /***********************开始设置build ab************************/
            //设置AB name
            foreach (var changedAsset in buildInfo.AssetDataMaps)
            {
                //根据改变的ChangedAssets,获取Asset的资源
                var key = changedAsset.Key;
                var asset = buildInfo.AssetDataMaps[changedAsset.Key];
                //设置ABName 有ab的则用ab ，没有就用configpath
                string abname = asset.ABName;
                //
                var ai = GetAssetImporter(key);
                if (ai)
                {
                    ai.assetBundleName = abname;
                }
            }

            //生成AssetBundle
            var platformOutputPath = Path.Combine(buildParams.OutputPath, BDApplication.GetPlatformPath(buildParams.Platform));
            string artOutputPath = IPath.Combine(platformOutputPath, BResources.ART_ROOT_PATH);
            //构建平台
            BuildTarget buildTarget = BuildTarget.Android;
            switch (buildParams.Platform)
            {
                case RuntimePlatform.Android:
                    buildTarget = BuildTarget.Android;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    buildTarget = BuildTarget.iOS;
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                {
                    buildTarget = BuildTarget.StandaloneWindows64;
                }
                    break;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                {
                    buildTarget = BuildTarget.StandaloneOSX;
                }
                    break;
            }
            try
            {
                AssetDatabase.RemoveUnusedAssetBundleNames();
                if (!Directory.Exists(artOutputPath))
                {
                    Directory.CreateDirectory(artOutputPath);
                }

                BuildPipeline.BuildAssetBundles(artOutputPath, buildParams.Options | BuildAssetBundleOptions.DeterministicAssetBundle, buildTarget);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            //清除AB Name
            RemoveAllAssetbundleName();
            AssetImpoterCacheMap.Clear();
            //删除无用文件
            var delFiles = Directory.GetFiles(artOutputPath, "*", SearchOption.AllDirectories);
            foreach (var df in delFiles)
            {
                var ext = Path.GetExtension(df);
                if (ext == ".meta" || ext == ".manifest")
                {
                    File.Delete(df);
                }
            }


            //移动老配置
            var buildInfoPath = Path.Combine( platformOutputPath , BResources.ART_CONFIG_PATH);
            if (File.Exists(buildInfoPath))
            {
                string oldBuildInfoPath =  Path.Combine( platformOutputPath , BResources.ART_OLD_CONFIG_PATH);
                File.Delete(oldBuildInfoPath);
                File.Move(buildInfoPath, oldBuildInfoPath);
            }

            //写入新配置
            FileHelper.WriteAllText(buildInfoPath, JsonMapper.ToJson(buildInfo));
            //BD生命周期触发
            BDEditorBehaviorHelper.OnEndBuildAssetBundle(platformOutputPath);
            AssetHelper.AssetHelper.GenPackageBuildInfo(platformOutputPath, buildParams.Platform);
        }


        static private Dictionary<string, AssetImporter> AssetImpoterCacheMap = new Dictionary<string, AssetImporter>();

        /// <summary>
        /// 获取assetimpoter
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static private AssetImporter GetAssetImporter(string path)
        {
            AssetImporter ai = null;
            if (!AssetImpoterCacheMap.TryGetValue(path, out ai))
            {
                ai = AssetImporter.GetAtPath(path);
                AssetImpoterCacheMap[path] = ai;
                if (!ai)
                {
                    Debug.LogError("【打包】获取资源失败:" + path);
                }
            }


            return ai;
        }


        /// <summary>
        /// 移除无效资源
        /// </summary>
        public static void RemoveAllAssetbundleName()
        {
            EditorUtility.DisplayProgressBar("资源清理", "清理中...", 1);

            foreach (var ai in AssetImpoterCacheMap)
            {
                if (ai.Value != null)
                {
                    if (!string.IsNullOrEmpty(ai.Value.assetBundleVariant))
                    {
                        ai.Value.assetBundleVariant = null;
                    }

                    ai.Value.assetBundleName = null;
                }
            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}
