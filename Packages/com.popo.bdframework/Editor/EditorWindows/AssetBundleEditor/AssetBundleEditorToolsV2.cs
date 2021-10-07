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
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace BDFramework.Editor.AssetBundle
{
    static public class AssetBundleEditorToolsV2
    {
        static string RUNTIME_PATH = "/runtime/";

        /// <summary>
        /// 生成AssetBundle
        /// </summary>
        /// <param name="outputPath">导出目录</param>
        /// <param name="target">平台</param>
        /// <param name="options">打包参数</param>
        /// <param name="isUseHashName">是否为hash name</param>
        public static bool GenAssetBundle(string outputPath, RuntimePlatform platform, bool isUseHashName = false)
        {
            var buildTarget = GetBuildTarget(platform);
            AssetBundleEditorToolsV2ForAssetGraph.Build(buildTarget, outputPath, isUseHashName);
            return true;
        }


        /// <summary>
        /// 资源类型配置
        /// </summary>
        static public Dictionary<ManifestItem.AssetTypeEnum, List<string>> AssetTypeConfigMap = new Dictionary<ManifestItem.AssetTypeEnum, List<string>>()
        {
            {ManifestItem.AssetTypeEnum.Prefab, new List<string>() {".prefab"}}, //Prefab
            {ManifestItem.AssetTypeEnum.SpriteAtlas, new List<string>() {".spriteatlas"}}, //Atlas
            {ManifestItem.AssetTypeEnum.Texture, new List<string>() {".jpg", ".jpeg", ".png", ".tga"}}, //Tex
            {ManifestItem.AssetTypeEnum.Mat, new List<string>() {".mat"}}, //mat
            {ManifestItem.AssetTypeEnum.TextAsset, new List<string>() {".json", ".xml", ".info", ".txt"}}, //TextAsset
            {ManifestItem.AssetTypeEnum.AudioClip, new List<string>() {".mp3", ".ogg", ".wav"}}, //sound
            {ManifestItem.AssetTypeEnum.Mesh, new List<string>() {".mesh"}}, //sound
            {ManifestItem.AssetTypeEnum.Font, new List<string>() {".fnt", ".fon", ".font", ".ttf", ".ttc", ".otf", ".eot",}}, //sound
        };


        #region 依赖关系

        static Dictionary<string, List<string>> DependenciesMap = new Dictionary<string, List<string>>();

        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static private string[] GetDependencies(string path)
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


        #region BuildTarget 和RuntimePlatform互转

        /// <summary>
        /// 获取AB构建平台
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static BuildTarget GetBuildTarget(RuntimePlatform platform)
        {
            //构建平台
            BuildTarget target = BuildTarget.Android;
            switch (platform)
            {
                case RuntimePlatform.Android:
                    target = BuildTarget.Android;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    target = BuildTarget.iOS;
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                {
                    target = BuildTarget.StandaloneWindows64;
                }
                    break;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                {
                    target = BuildTarget.StandaloneOSX;
                }
                    break;
            }

            return target;
        }

        /// <summary>
        /// 获取runtimeplatform
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static RuntimePlatform GetRuntimePlatform(BuildTarget buildTarget)
        {
            var platform = RuntimePlatform.Android;
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    platform = RuntimePlatform.Android;
                }
                    break;
                case BuildTarget.iOS:
                {
                    platform = RuntimePlatform.IPhonePlayer;
                }
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                {
                    platform = RuntimePlatform.WindowsPlayer;
                }
                    break;
                case BuildTarget.StandaloneOSX:
                {
                    platform = RuntimePlatform.OSXPlayer;
                }
                    break;
            }

            return platform;
        }

        #endregion

        #region 资源测试相关

        /// <summary>
        /// 获取资源类型
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        static public ManifestItem.AssetTypeEnum GetAssetType(string assetPath)
        {
            var ext = Path.GetExtension(assetPath);
            foreach (var item in AssetTypeConfigMap)
            {
                if (item.Value.Contains(ext))
                {
                    return item.Key;
                }
            }

            return ManifestItem.AssetTypeEnum.Others;
        }

        /// <summary>
        /// 测试加载所有的AssetBundle
        /// </summary>
        static public void TestLoadAllAssetbundle(string abPath)
        {
            
            UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
            //初始化BResource
            BResources.Load(AssetLoadPath.StreamingAsset, abPath);
            //加载
            var allRuntimeAssets = BDApplication.GetAllRuntimeAssetsPath();
            foreach (var asset in allRuntimeAssets)
            {
                var type = GetAssetType(asset);
                var idx = asset.IndexOf(RUNTIME_PATH, StringComparison.OrdinalIgnoreCase);
                var runtimePath = asset.Substring(idx + RUNTIME_PATH.Length);
                runtimePath = runtimePath.Replace(Path.GetExtension(runtimePath), "");
                //Debug.Log("【LoadTest】:" + runtimePath);
                switch (type)
                {
                    case ManifestItem.AssetTypeEnum.Prefab:
                    {
                        try
                        {
                            //加载
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            var obj = BResources.Load<GameObject>(runtimePath);
                            sw.Stop();
                            var loadtime = sw.ElapsedMilliseconds;
                            //实例化
                            sw.Restart();
                            var gobj = GameObject.Instantiate(obj);
                            sw.Stop();
                            var instantTime = sw.ElapsedMilliseconds;
                            Debug.LogFormat("<color=yellow>【LoadTest】:{0}</color> <color=green>【加载耗时】:{1};【初始化耗时】:{2}</color>", runtimePath, loadtime, instantTime);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("【LoadTest】加载失败:" + runtimePath);
                        }
                    }
                        break;
                    case ManifestItem.AssetTypeEnum.TextAsset:
                    {
                        //测试打印AssetText资源
                        var textAsset = BResources.Load<TextAsset>(runtimePath);
                        Debug.Log(textAsset.text);
                    }
                        break;
                }
            }
        }

        #endregion
    }
}
