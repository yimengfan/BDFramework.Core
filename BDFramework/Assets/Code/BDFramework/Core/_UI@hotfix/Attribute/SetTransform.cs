using System;

namespace BDFramework.UI
{
    public class SetTransform : Attribute
    {
        public string Path;

        public SetTransform(string path)
        {
            this.Path = path;
        }
    }
}