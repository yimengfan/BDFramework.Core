using System;

namespace BDFramework.UI
{
    public class BSetTransform : Attribute
    {
        public string Path;

        public BSetTransform(string path)
        {
            this.Path = path;
        }
    }
}