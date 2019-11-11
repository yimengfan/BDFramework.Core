using System;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 节点发现属性
    /// </summary>
    public class TransformPath : Attribute
    {
        public string Path;

        public TransformPath(string path)
        {
            this.Path = path;
        }
    }
}