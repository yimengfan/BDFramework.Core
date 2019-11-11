using BDFramework.UFlux;
using BDFramework.UFlux.item;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Game.demo6_UFlux
{
    [UI((int)UFluxWindowEnum.Test002,"Windows/UFlux/demo002/Window_FluxTest002")]
    public class Window_Test002 : AWindow<Props_WindowTest002>
    {
        public Window_Test002(string path) : base(path)
        {
        }

        [TransformPath("btn_AddItem")]
        private Button btn_AddItem;
        [TransformPath("btn_RemoveItem")]
        private Button btn_RemoveItem;
        [TransformPath("btn_Close")]
        private Button btn_Close;
        public override void Init()
        {
            base.Init();
            btn_AddItem.onClick.AddListener(Onclick_AddItem);
            btn_RemoveItem.onClick.AddListener(Onclick_RemoveItem);
            btn_Close.onClick.AddListener(Onclick_Close);
        }


        /// <summary>
        /// 点击事件
        /// </summary>
        private void Onclick_AddItem()
        {
            var item = new Props_ItemTest002();
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
            this.SetProps();
        }

        /// <summary>
        /// 移除Item
        /// </summary>
        private void Onclick_RemoveItem()
        {
            if(this.Props.CotentList.Count==0)return;
            
            this.Props.CotentList.RemoveAt(this.Props.CotentList.Count-1);
            this.Props.SetPropertyChange(nameof(this.Props.CotentList));//设置属性更改
            this.SetProps();
        }


        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void Onclick_Close()
        {
            this.Close();
        }
        
        
    }
}