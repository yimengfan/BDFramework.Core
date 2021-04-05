using System;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 节点自动赋值
    /// </summary>
    public class TransformPathAttribute : UFluxAttribute
    {
        public string Path;

        public TransformPathAttribute(string path)
        {
            this.Path = path;
        }


        public override void Do(Transform root, object fieldValue)
        {
            base.Do(root, fieldValue);
        }
    }
}