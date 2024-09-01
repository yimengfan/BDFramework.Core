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
    public class StatusListenerServer
    {

        static private Dictionary<string, StatusListenerService> serviceMap = new Dictionary<string, StatusListenerService>();

        /// <summary>
        /// 创建一个service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static StatusListenerService Create(string name)
        {
            var data = new StatusListenerService();

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
        public static StatusListenerService GetService(string name)
        {
            StatusListenerService status = null;
            serviceMap.TryGetValue(name, out status);
            if (status == null)
            {
                status = new StatusListenerService();

                serviceMap[name] = status;
            }
            return status;
        }

        /// <summary>
        /// 删除一个service
        /// </summary>
        /// <param name="name"></param>
        static public void RemoveService(string name)
        {
            StatusListenerService status = null;
            serviceMap.TryGetValue(name, out status);
            status = null;
            //
            serviceMap.Remove(name);
        }

        /// <summary>
        /// 删除一个service
        /// </summary>
        /// <param name="name"></param>
        static public void RemoveALLService()
        {
            serviceMap.Clear();
        }

        #region   

        
        
        static private Dictionary<string, ADataListenerTBase> serviceTMap = new Dictionary<string, ADataListenerTBase>();

        /// <summary>
        /// 创建一个service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ADataListenerT<T> Create<T>(string name)
        {
            ADataListenerTBase data = null;
            
            if (!serviceTMap.TryGetValue(name, out data))
            {
                data = new ADataListenerT<T>();
                serviceTMap[name] = data;
            }

            return data  as  ADataListenerT<T>;
        }
        
        /// <summary>
        /// 获取一个service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static  ADataListenerT<T>  GetService<T>(string name)
        {
            ADataListenerTBase data = null;
            serviceTMap.TryGetValue(name, out data);
            if (data == null)
            {
                data = new ADataListenerT<T>();

                serviceTMap[name] = data;
            }
            return data as  ADataListenerT<T>;
        }

        
        

        #endregion
        
        
    }

}