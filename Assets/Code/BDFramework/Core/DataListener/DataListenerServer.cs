using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace BDFramework.DataListener
{
    /// <summary>
    /// 做数据驱动服务
    /// </summary>
    public class DataListenerServer
    {

        static private Dictionary<string, DataListenerService> serviceMap = new Dictionary<string, DataListenerService>();

        /// <summary>
        /// 创建一个service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DataListenerService Create(string name)
        {
            var data = new DataListenerService();

            if (serviceMap.ContainsKey(name) == false)
            {
                serviceMap[name] = data;
            }
            else
            {
                return GetService(name);
            }

            return data;
        }

        /// <summary>
        /// 获取一个service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DataListenerService GetService(string name)
        {
            DataListenerService data = null;
            serviceMap.TryGetValue(name, out data);
            if (data == null)
            {
                data = new DataListenerService();

                serviceMap[name] = data;
            }
            return data;
        }

        /// <summary>
        /// 删除一个service
        /// </summary>
        /// <param name="name"></param>
        static public void DelService(string name)
        {
            DataListenerService data = null;
            serviceMap.TryGetValue(name, out data);
            data = null;
            //
            serviceMap.Remove(name);
        }

        /// <summary>
        /// 删除一个service
        /// </summary>
        /// <param name="name"></param>
        static public void DelALLService()
        {
            serviceMap.Clear();
        }
    }

}