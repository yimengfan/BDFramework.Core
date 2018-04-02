using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
/// <summary>
/// 做数据驱动服务
/// </summary>
public class DataDriveServer
{

    static private Dictionary<string, DataDrive_Service> serviceMap = new Dictionary<string, DataDrive_Service>();
    /// <summary>
    /// 创建一个service
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static DataDrive_Service Create(string name)
    {
        var data = new DataDrive_Service();

        if( serviceMap.ContainsKey(name) == false)
        {
            serviceMap[name] = data;
        }
        else
        {
            Debug.LogError("已存在同名DataDrive_Service");
            return null;
        }
        return data;
    }
    /// <summary>
    /// 获取一个service
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static DataDrive_Service GetService(string name)
    {
        DataDrive_Service data = null;

        serviceMap.TryGetValue(name, out data);
        return data;
    }
   /// <summary>
   /// 删除一个service
   /// </summary>
   /// <param name="name"></param>
   static public void DelService(string name)
    {
        DataDrive_Service data = null;
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

