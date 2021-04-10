using System.Collections;
using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace Game.demo6_UFlux
{
    public class APropsTest001 : APropsBase
    {
        //这里进行数据的绑定
        [ComponentValueBind( "chatbox/head", typeof(Image),nameof(Image.overrideSprite))]//数据赋值对象
        public string headImg = "";
        
        //这里进行数据的绑定
        [ComponentValueBind("chatbox/content",typeof(Text),nameof(Text.text))]//数据赋值对象
        public string content = "";

    }
    
    //这里是Component标签，用以创建时候进行绑定Transform
    [Component("Windows/UFlux/demo001/Component_test01",false)] 
    public class Component_Test001 : ATComponent<APropsTest001>
    {
        public override void Open()
        {
            base.Open();
            //执行 test logic
            IEnumeratorTool.StartCoroutine(IE_DoTestLogic());
        }

        
        List<string> contentList =new List<string>()
        {
            "大姐姐好!",
            "我要一份大姐姐的爱~",
            "风间，你这个禽兽~",
            "美亚，你这个阴险的老巫婆~",
            "嘿嘿嘿，小白~~",
            "大象，大象你的鼻子怎么那么长?",
            "大姐姐，我能看看你的内裤么",
        };

        IEnumerator IE_DoTestLogic()
        {
            for (int i = 0; i < 5; i++)
            {
               
                //这里对当前数据进行赋值
                this.Props.headImg = "Image/" + (int)Random.Range(1f, 10.9f);
                this.Props.content = contentList[(int) Random.Range(0f, contentList.Count)];
                //这里是手动设置修改版本
                //设置属性修改
                this.Props.SetPropertyChange(nameof(this.Props.headImg));
                this.Props.SetPropertyChange(nameof(this.Props.content));
                //提交修改数据
                this.CommitProps();
                yield return new WaitForSeconds(2);
            }
           
            //关闭并且销毁
            this.Close();
            this.Destroy();
            yield break;
        }
    }
}