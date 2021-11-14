using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// DevOps工具
    /// </summary>
    static public class DevOpsTools
    {
        /// <summary>
        /// 拷贝发布资源
        /// </summary>
        static public void CopyPublishAssetTo(string targetpath , RuntimePlatform platform)
        {
            List<string> blackFile = new List<string>() { "/Build.info", ".manifest" };
            //清空目标文件夹
            Directory.Delete(targetpath,true);
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
                    Debug.Log("[黑名单]"+ ret);
                    continue;
                }
                //
                var tp = _f.Replace(sourcepath, targetpath);
                FileHelper.Copy(_f,tp ,true);
            }
        }
    }
}