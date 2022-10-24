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