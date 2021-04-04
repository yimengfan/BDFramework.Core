using System;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 节点自动赋值
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