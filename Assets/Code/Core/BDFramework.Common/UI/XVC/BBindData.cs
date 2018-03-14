using System;

namespace BDFramework.UI
{
    public class BBindData : Attribute
    {
        public string Path;

        public BBindData(string path)
        {
            this.Path = path;
        }
    }
}