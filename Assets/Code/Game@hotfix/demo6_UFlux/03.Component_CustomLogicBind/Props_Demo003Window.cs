using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;

namespace Game.demo6_UFlux._05.NodeHelper
{
    public class Props_Demo003Window : PropsBase
    {   
        /// <summary>
        /// 绑定Equipments 所有子元素
        /// </summary>
        [TransformPath("Equipments")]
        public List<Props_Demo003Item> StarItems = new List<Props_Demo003Item>();
        
        /// <summary>
        /// 绑定OneNodeChange节点为PropsDemo003Item
        /// </summary>
        [TransformPath("OneNodeChange")]
        public Props_Demo003Item OneNodeChange;
        
        
      
        /// <summary>
        /// 值转换成执行逻辑
        /// </summary>
        [TransformPath("Equipments")]
        [ComponentValueBind(typeof(TransformChild),nameof(TransformChild.ShowHideChildByNumber))]
        public int value;
    }
}