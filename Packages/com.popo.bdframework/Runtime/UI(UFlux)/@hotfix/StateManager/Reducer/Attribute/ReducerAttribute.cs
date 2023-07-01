using System;

namespace BDFramework.UFlux.Reducer
{
    /// <summary>
    /// ReduderMethod的Attribute
    /// </summary>
    public class ReducerAttribute :  Attribute
    {
        public int ReducerEnum { get; private set; } = -1;

        public ReducerAttribute(int @enum)
        {
            this.ReducerEnum = @enum;
        }
    }
}