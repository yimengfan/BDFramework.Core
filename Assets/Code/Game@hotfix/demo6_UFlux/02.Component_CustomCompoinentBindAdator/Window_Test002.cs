using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.item;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.demo6_UFlux
{
    /// <summary>
    /// props
    /// </summary>
    public class APropsWindowTest002 : APropsBase
    {
        [ComponentValueBind("ScrollView",typeof(ScrollRectAdaptor), nameof(ScrollRectAdaptor.Contents))]//数据赋值对象
        public List<APropsItemTest002>  CotentList =new List<APropsItemTest002>();
    }
    
    /// <summary>
    /// 窗口2测试
    /// </summary>
    [UI((int)WinEnum.Win_Demo6_Test002,"Windows/UFlux/demo002/Window_FluxTest002")]
    public class Window_Test002 : AWindow<APropsWindowTest002>
    {
        public Window_Test002(string path) : base(path)
        {
        }
        
        public override void Init()
        {
            base.Init();
        }


        /// <summary>
        /// 点击事件
        /// </summary>
        [ButtonOnclick("btn_AddItem")]
        private void Onclick_AddItem()
        {
            var item = new APropsItemTest002();
            int id = this.Props.CotentList.Count;
            //填充数据 
            item.ComponentType = typeof(Component_ItemTest002);//该item的组件，用于自动赋值时候用
            var rand = (int) UnityEngine.Random.Range(1f, 10.9f);
            item.ItemImg ="Image/" +rand;
            item.Content = rand + "/10";
            item.ID = "Id:" + id;
            item.Action = () =>
            {
                Debug.Log("购买道具,id:"+  id);
            };
            item.SetAllPropertyChanged();  //设置所有属性更改
            //
            this.Props.CotentList.Add(item);
            this.Props.SetPropertyChange(nameof(this.Props.CotentList));//设置属性更改
            this.CommitProps();
        }

        /// <summary>
        /// 移除Item
        /// </summary>
        [ButtonOnclick("btn_RemoveItem")]
        private void Onclick_RemoveItem()
        {
            if(this.Props.CotentList.Count==0)return;
            
            this.Props.CotentList.RemoveAt(this.Props.CotentList.Count-1);
            this.Props.SetPropertyChange(nameof(this.Props.CotentList));//设置属性更改
            this.CommitProps();
        }


        /// <summary>
        /// 关闭窗口
        /// </summary>
        [ButtonOnclick("btn_Close")]
        private void Onclick_Close()
        {
            this.Close();
        }
        
        
    }
}