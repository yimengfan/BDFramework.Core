using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace BDFramework.UFlux
{
    public class AWindow: AWindow<PropsBase>
    {
        public AWindow(string path) : base(path)
        {
        }

        public AWindow(Transform transform) : base(transform)
        {
        }
    }
}