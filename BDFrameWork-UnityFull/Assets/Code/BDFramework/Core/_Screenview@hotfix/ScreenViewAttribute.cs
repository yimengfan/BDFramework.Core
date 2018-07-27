using System;
using BDFramework.Mgr;

namespace BDFramework.ScreenView
{
    public class ScreenViewAttribute: ManagerAtrribute
    {

        public bool IsDefault { get; private set; }
        public ScreenViewAttribute(string name, bool isDefault =false) :base(name)
        {
            this.IsDefault = isDefault;
        }
    }
}