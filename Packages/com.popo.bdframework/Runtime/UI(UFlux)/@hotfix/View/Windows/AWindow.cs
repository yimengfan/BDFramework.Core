using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 窗口基类 不带Props
    /// </summary>
    public class AWindow: AWindow<NoProps>
    {
        public AWindow(string path) : base(path)
        {
        }

        public AWindow(Transform transform) : base(transform)
        {
        }
    }
}