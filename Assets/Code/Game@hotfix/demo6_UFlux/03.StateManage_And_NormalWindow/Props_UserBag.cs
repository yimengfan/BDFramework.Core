using System.Collections.Generic;
using BDFramework.UFlux.View.Props;
using BDFramework.UI.Demo_ScreenRect;

namespace BDFramework.UFlux.Test
{
    public class Props_UserBag: PropsBase
    {
        
        [TransformPath("ScrollView")]
        [ComponentValueBind(typeof(DemoScrollRect), nameof(DemoScrollRect.Contents))]
        public List<Props_ItemTest003>  ItemList=new List<Props_ItemTest003>();
    }
}