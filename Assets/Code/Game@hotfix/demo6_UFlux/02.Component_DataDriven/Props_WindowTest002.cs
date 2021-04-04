using System;
using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.item;
using BDFramework.UFlux.View.Props;
using BDFramework.UI.Demo_ScreenRect;
using UnityEngine.UI;

namespace Game.demo6_UFlux
{
    public class test : Attribute
    {
        public test(Type t)
        {
            
        }
    }
    public class Props_WindowTest002 : PropsBase
    {
        [TransformPath("ScrollView")]
        [ComponentValueBind(nameof(ScrollRectAdaptor), nameof(ScrollRectAdaptor.Contents))]//数据赋值对象
        public List<Props_ItemTest002>  CotentList =new List<Props_ItemTest002>();
    }
}