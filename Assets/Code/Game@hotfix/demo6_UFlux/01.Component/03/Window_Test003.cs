using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using Game.demo6_UFlux.Component;
using UnityEngine;
using UnityEngine.UI;

namespace Game.demo6_UFlux.Comonent._03
{
    public class Props_Window : APropsBase
    {
        /// <summary>
        ///嵌套： 绑定单个节点
        /// </summary>
        [ComponentValueBind("Item",typeof(UFluxBindLogic), nameof(UFluxBindLogic.BindChild))]
        public Props_Item Item= new Props_Item();
        
        /// <summary>
        ///嵌套： 绑定到每个子节点的
        /// </summary>
        [ComponentValueBind("Items",typeof(UFluxBindLogic), nameof(UFluxBindLogic.BindChildren))]
        public PropsList<Props_Item> ItemList = new PropsList<Props_Item>();

    }

    [UI((int) WinEnum.Win_UFlux_01Component_03, "Windows/UFlux/01Component/Window_Test003")]
    public class Window_Test003 : AWindow<Props_Window>
    {
        public Window_Test003(string path) : base(path)
        {
        }
        
        [ButtonOnclick("btn_BindSingle")]
        private void btn_BindSingle()
        {
            int i = Random.Range(1, 6);
            this.Props.Item.IconPath = "Image/" + i;
            this.Props.Item.IconName = "小新被刷新:" + i;
            //嵌套的父级Class最好，手动标记修改
            this.Props.SetPropertyChange(nameof(this.Props.Item));
            this.CommitProps();
            
            Debug.Log("嵌套绑定单节点,点击跟踪代码");
        }

        [ButtonOnclick("btn_BindList")]
        private void btn_BindList()
        {
         
            for (int i = 0;  i < 6; i++)
            {
                var item = new Props_Item();
                item.IconPath = "Image/1";
                item.IconName = "小新" + i + "号";
                this.Props.ItemList.Add(item);
            }
            //嵌套的父级Class最好，手动标记修改
            this.Props.SetPropertyChange(nameof(this.Props.ItemList));
            this.CommitProps();
            
            Debug.Log("嵌套绑定多节点,点击跟踪代码");
        }
        
        
        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            this.Close();
        }
    }
}