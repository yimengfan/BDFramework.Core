using UnityEngine;

namespace BDFramework.Configure
{
    /// <summary>
    /// 配置数据基类
    /// </summary>
    abstract public class ConfigDataBase
    {
        /// <summary>
        /// class类型
        /// </summary>
        [HideInInspector]
        public string ClassType;
    }
}
