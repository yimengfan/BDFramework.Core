using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace BDFramework.VersionContrller
{
    public class AssetConfig
    {
        public string Platfrom = "";
        public double Version = 0.1d;
        public List<AssetItem> Assets = new List<AssetItem>();
    }

    public class AssetItem
    {
        public string HashName = "";
        public string LocalPath = "";
    }
    
    /// <summary>
    /// 版本控制类
    /// </summary>
    static public class VersionContorller
    {
        
        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="configPath">配置的路径,网络,本地</param>
        /// <param name="onProcess"></param>
        /// <param name="onError"></param>
        /// <param name="onScucess"></param>
        static public void Start(string configPath, Action<int,int>onProcess, Action<string> onError, Action<string> onScucess)
        {
         
            
        }   
    }
}