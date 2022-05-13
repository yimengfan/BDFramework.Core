using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using NUnit.Framework.Internal;
using UnityEngine;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// 调用Editor接口
    /// 函数为Static
    /// EditorInvoke/{func_fullname}
    /// EditorInvoke/namepace.classname.funcktionname
    /// </summary>
    public class WP_EditorInvoke : IWebApiProccessor
    {
        public string WebApiName { get; set; } = "EditorInvoke";


        private Dictionary<string, Action> actionCache = new Dictionary<string, Action>();

        public void WebAPIProccessor(string apiParams, HttpListenerResponse response)
        {
            var lastDotIdx = apiParams.LastIndexOf(".");

            if (lastDotIdx == -1)
            {
                throw new Exception("Function不存在");
            }

            var clasname = apiParams.Substring(0, lastDotIdx);
            var funcname = apiParams.Substring(lastDotIdx + 1);
            //获取type
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type type = null;
            foreach (var assembly in assemblies)
            {
                type = assembly.GetTypes().FirstOrDefault((t) => t.FullName.Equals(clasname, StringComparison.OrdinalIgnoreCase));
                if (type != null)
                {
                    break;
                }
            }
            //
            var method = type.GetMethod(funcname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, null);
            }
        }


        /// <summary>
        /// 测试接口
        /// </summary>
        static private void Test()
        {
            Debug.Log("EditorInvoke Success!");
        }
    }
}
