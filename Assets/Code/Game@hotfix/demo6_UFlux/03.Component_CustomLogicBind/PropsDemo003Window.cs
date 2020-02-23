using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace Code.Game.demo6_UFlux._05.NodeHelper
{
    public class PropsDemo003Window : PropsBase
    {   
        /// <summary>
        /// 绑定Equipments 所有子元素
        /// </summary>
        [TransformPath("Equipments")]
        public List<PropsDemo003Item> StarItems = new List<PropsDemo003Item>();
        
        /// <summary>
        /// 绑定OneNodeChange节点为PropsDemo003Item
        /// </summary>
        [TransformPath("OneNodeChange")]
        public PropsDemo003Item OneNodeChange;
        
        
      
        /// <summary>
        /// 值转换成执行逻辑
        /// </summary>
        [TransformPath("Equipments")]
        [ComponentValueBind(typeof(TransformChild),nameof(TransformChild.ShowHideChildByNumber))]
        public int value;
    }
}