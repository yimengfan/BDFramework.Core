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
                    return typeof(Texture2D);
                }
            }
            return type;
        }
        
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
    }
}
