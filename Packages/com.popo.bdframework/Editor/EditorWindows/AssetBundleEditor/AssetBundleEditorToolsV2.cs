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
        static  public string RUNTIME_PATH = "/runtime/";

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
        static public Dictionary<AssetBundleItem.AssetTypeEnum, List<string>> AssetTypeConfigMap = new Dictionary<AssetBundleItem.AssetTypeEnum, List<string>>()
        {
            {AssetBundleItem.AssetTypeEnum.Prefab, new List<string>() {".prefab"}}, //Prefab
            {AssetBundleItem.AssetTypeEnum.SpriteAtlas, new List<string>() {".spriteatlas"}}, //Atlas
            {AssetBundleItem.AssetTypeEnum.Texture, new List<string>() {".jpg", ".jpeg", ".png", ".tga"}}, //Tex
            {AssetBundleItem.AssetTypeEnum.Mat, new List<string>() {".mat"}}, //mat
            {AssetBundleItem.AssetTypeEnum.Shader, new List<string>() {".shader"}}, //mat
            {AssetBundleItem.AssetTypeEnum.TextAsset, new List<string>() {".json", ".xml", ".info", ".txt"}}, //TextAsset
            {AssetBundleItem.AssetTypeEnum.AudioClip, new List<string>() {".mp3", ".ogg", ".wav"}}, //sound
            {AssetBundleItem.AssetTypeEnum.Mesh, new List<string>() {".mesh"}}, //sound
            {AssetBundleItem.AssetTypeEnum.Font, new List<string>() {".fnt", ".fon", ".font", ".ttf", ".ttc", ".otf", ".eot",}}, //sound
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
        static public AssetBundleItem.AssetTypeEnum GetAssetType(string assetPath)
        {
            var ext = Path.GetExtension(assetPath);
            foreach (var item in AssetTypeConfigMap)
            {
                if (item.Value.Contains(ext))
                {
                    return item.Key;
                }
            }

            return AssetBundleItem.AssetTypeEnum.Others;
        }
        
        #endregion
    }
}
