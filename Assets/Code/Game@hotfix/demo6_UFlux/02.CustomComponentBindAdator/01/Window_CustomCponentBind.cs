using BDFramework.UFlux;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using UnityEngine;

namespace Game.demo6_UFlux.CustomCponentBindAdaptor
{
    /// <summary>
    /// props
    /// </summary>
    public class Props_Windows : APropsBase
    {
        /// <summary>
        /// 列表的数据结构
        /// 容器必须为PropsList，元素必须为Props
        /// </summary>
        [ComponentValueBind("ScrollView",typeof(ScrollRectAdaptor), nameof(ScrollRectAdaptor.ContentMap))]
        public PropsList<Props_ItemTest002>  CotentList =new PropsList<Props_ItemTest002>();
    }
    
    /// <summary>
    /// 窗口2测试
    /// </summary>
    [UI((int)WinEnum.Win_UFlux_Test002,"Windows/UFlux/02CustomComponentBindAdator/01/Window_Test")]
    public class Window_CustomCponentBind : AWindow<Props_Windows>
    {
        public Window_CustomCponentBind(string path) : base(path)
        {
        }


        /// <summary>
        /// 点击事件
        /// </summary>
        [ButtonOnclick("btn_AddItem")]
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
            //
            this.Props.CotentList.Add(item);
            this.CommitProps();
        }

        
        /// <summary>
        /// 修改Item
        /// </summary>
        [ButtonOnclick("btn_ChangeItem")]
        private void Onclick_ChangeItem()
        {
            if(this.Props.CotentList.Count==0)return;
            //随机修改一个数据
            var idx = (int)UnityEngine.Random.Range(0, this.Props.CotentList.Count);
            var item = this.Props.CotentList.Get(idx);
            var imgIdx = (int)UnityEngine.Random.Range(1,10);
            item.ItemImg = "Image/" + imgIdx;
            //设置好修改数据
            this.Props.CotentList.SetChangedData(item);
            this.CommitProps();
        }

        
        /// <summary>
        /// 移除Item
        /// </summary>
        [ButtonOnclick("btn_RemoveItem")]
        private void Onclick_RemoveItem()
        {
            if(this.Props.CotentList.Count==0)return;
            var idx = (int)UnityEngine.Random.Range(0, this.Props.CotentList.Count);
            this.Props.CotentList.RemoveAt(idx);
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