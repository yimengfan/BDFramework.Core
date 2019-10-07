using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.item;
using BDFramework.UFlux.View.Props;
using BDFramework.UI.Demo_ScreenRect;

namespace Code.Game.demo6_UFlux
{
    public class Props_WindowTest002 : PropsBase
    {
        [TransformPath("ScrollView")]
        [ComponentValueBind(typeof(DemoScrollRect), nameof(DemoScrollRect.Contents))]//数据赋值对象
        public List<Props_ItemTest002>  CotentList =new List<Props_ItemTest002>();
    }
}