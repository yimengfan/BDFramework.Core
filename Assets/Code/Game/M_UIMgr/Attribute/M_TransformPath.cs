using System;

namespace Game.UI
{
    public class M_TransformPath : Attribute
    {
        public string Path;

        public M_TransformPath(string path)
        {
            this.Path = path;
        }
    }
}