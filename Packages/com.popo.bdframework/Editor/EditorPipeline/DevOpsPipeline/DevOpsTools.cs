using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.ResourceMgr;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// DevOps工具
    /// </summary>
    static public class DevOpsTools
    {
        #region 资产操作类
        /// <summary>
        /// 拷贝发布资源
        /// </summary>
        static public void CopyPublishAssetsTo(string targetpath, RuntimePlatform platform)
        {
            List<string> blackFile = new List<string>() {"/Build.info", ".manifest"};
            //清空目标文件夹
            if (Directory.Exists(targetpath))
            {
                Directory.Delete(targetpath, true);
            }

            //合并路径
            var sourcepath = IPath.Combine(BDApplication.DevOpsPublishAssetsPath, BDApplication.GetPlatformPath(platform));
            targetpath = IPath.Combine(targetpath, BDApplication.GetPlatformPath(platform));
            //TODO SVN更新资源

            //拷贝
            var files = Directory.GetFiles(sourcepath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var _f = file.Replace("\\", "/");
                var ret = blackFile.Find((blackstr) => _f.EndsWith(blackstr, StringComparison.OrdinalIgnoreCase));
                if (ret != null)
                {
                    // Debug.Log("[黑名单]" + _f);
                    continue;
                }

                //
                var tp = _f.Replace(sourcepath, targetpath);
                FileHelper.Copy(_f, tp, true);
            }
        }

        /// <summary>
        /// 删除拷贝的资源
        /// </summary>
        /// <param name="targetpath"></param>
        /// <param name="platform"></param>
        static public void DeleteCopyAssets(string targetpath, RuntimePlatform platform)
        {
            targetpath = IPath.Combine(targetpath, BDApplication.GetPlatformPath(platform));
            //优先删除拷贝的美术资源，防止构建完再导入  其他资源等工作流完全切入DevOps再进行删除
            var copyArtPath = IPath.Combine(targetpath, BResources.ASSET_ROOT_PATH);
            if (Directory.Exists(copyArtPath))
            {
                Directory.Delete(copyArtPath, true);
            }
        }

        #endregion


        #region CI相关接口

        /// <summary>
        /// 获取所有的Ci接口
        /// </summary>
        static public List<MethodInfo> GetCIApis()
        {
            var assembly = typeof(BDFrameworkAssetsEnv).Assembly;
            var ciMethodList = new List<MethodInfo>();
            //Type
            foreach (var type in assembly.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<CIAttribute>(false);
                    if (attr != null)
                    {
                        ciMethodList.Add(method);
                    }
                }
            }

            return ciMethodList;
        }

        #endregion
    }
}