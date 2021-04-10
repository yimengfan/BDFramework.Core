using System;

namespace BDFramework.UFlux.View.Props
{
    /// <summary>
    /// 每个component的Prop
    /// 1.Prop的成员变量如果是复合类型，则类型必须为Prop.
    ///  如List<Props> Dictionnary<int,Props> 
    /// </summary>
    abstract  public class APropsBase : AStateBase
    {
        /// <summary>
        /// 这里是描述ComponentType的类型，
        /// </summary>
        public Type ComponentType { get; set; }= null;

    }
}