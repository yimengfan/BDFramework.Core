using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace Code.Game.demo6_UFlux._05.NodeHelper
{
    public class PropsDemo003Window : PropsBase
    {   
        [TransformPath("Equipments")]
        public List<PropsDemo003Item> StarItems = new List<PropsDemo003Item>();
        
        [TransformPath("OneNodeChange")]
        public PropsDemo003Item OneNodeChange;
        
        
        [TransformPath("Equipments")]
        [ComponentValueBind(typeof(TransformHelper),nameof(TransformHelper.ShowHideChildByNumber))]
        public int ShowHideChildByNumber;
    }
}