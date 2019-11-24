using System;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 节点发现属性
    /// </summary>
    public class ComponentAttribute : Attribute
    {
        public string Path{ get; private set; }

        public bool IsAsyncLoad { get; private set; }
        public ComponentAttribute(string path ,bool isAsyncLoad =false)
        {
            this.Path = path;
            this.IsAsyncLoad = isAsyncLoad;
        }
    }
}