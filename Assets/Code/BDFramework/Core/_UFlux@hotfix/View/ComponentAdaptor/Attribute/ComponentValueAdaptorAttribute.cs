using System;

namespace BDFramework.UFlux
{
    public class ComponentValueAdaptorAttribute : Attribute
    {
        public string  FieldName { get; private set; }

        public ComponentValueAdaptorAttribute(string fn)
        {
            this.FieldName = fn;
        }
    }
}