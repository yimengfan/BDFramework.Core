using System;
using BDFramework.Mgr;

namespace BDFramework.UFlux
{
    public class ComponentAdaptorProcessAttribute : ManagerAtrribute
    {
        public Type ComponentType;
        public ComponentAdaptorProcessAttribute(Type t):base(t.Name)
        {
            this.ComponentType = t;
        }

    }
}