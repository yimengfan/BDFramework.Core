using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFramework.Mgr;

namespace BDFramework.UI
{
    [Obsolete("please use new uiframe: uflux.")]
    public class UIAttribute : ManagerAtrribute
    {
        public string ResourcePath { get; private set; }
       
        public UIAttribute(int intTag, string resPath):base(intTag)
        {
            this.ResourcePath = resPath;
        }
    }
}
