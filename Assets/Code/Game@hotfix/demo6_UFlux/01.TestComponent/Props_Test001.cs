using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace Code.Game.demo6_UFlux
{
    public class Props_Test001 : PropsBase
    {
        //这里进行数据的绑定
        [TransformPath("chatbox/head")]  //节点
        [ComponentValueBind(typeof(Image), nameof(Image.overrideSprite))]//数据赋值对象
        public string headImg = "";
        //这里进行数据的绑定
        [TransformPath("chatbox/content")] //节点
        [ComponentValueBind(typeof(Text), nameof(Text.text))]//数据赋值对象
        public string content = "";

    }
}