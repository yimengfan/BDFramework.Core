using System.Collections;
using System.Collections.Generic;
using BDFramework.UFlux;
using UnityEngine;

namespace Game.demo6_UFlux
{
    
    //这里是Component标签，用以创建时候进行绑定Transform
    [Component("Windows/UFlux/demo001/Component_test01")] 
    public class Component_Test001 : Component<Props_Test001>
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
                this.Props.headImg = "Image/" + (int)UnityEngine.Random.Range(1f, 10.9f);
                this.Props.content = contentList[(int) UnityEngine.Random.Range(0f, contentList.Count)];
                //设置属性修改，
                //可以不设置，但是默认的对比算法，每次都要反射 对比所有值，效率很差
                //遇到大规模数据，嵌套，效率会更差
                this.Props.SetPropertyChange(nameof(this.Props.headImg));
                this.Props.SetPropertyChange(nameof(this.Props.content));
                //提交修改数据
                this.SetProps();
                yield return new WaitForSeconds(2);
            }
           
            //关闭并且销毁
            this.Close();
            this.Destroy();
            yield break;
        }
    }
}