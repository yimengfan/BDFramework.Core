using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using Game.demo6_UFlux;
using Game.demo6_UFlux._05.NodeHelper;
using UnityEngine;
using UnityEngine.UI;

namespace Game.demo6_UFlux
{
    public class APropsDemo003Window : APropsBase
    {
        /// <summary>
        /// 绑定Equipments 所有子元素
        /// </summary>
        [ComponentValueBind("Equipments",typeof(UFluxAutoLogic), nameof(UFluxAutoLogic.ForeachSetChildValue))]
        public ComponentList<PropsDemo003Item> StarItems = new ComponentList<PropsDemo003Item>();
        /// <summary>
        /// 绑定OneNodeChange节点为PropsDemo003Item
        /// </summary>
        [ComponentValueBind("OneNodeChange",typeof(UFluxAutoLogic), nameof(UFluxAutoLogic.SetChildValue))]
        public PropsDemo003Item OneNodeChange;
        /// <summary>
        /// 值转换成执行逻辑
        /// </summary>
        [ComponentValueBind("Equipments",typeof(TransformChild), nameof(TransformChild.ShowHideChildByNumber))]
        public int value;
    }

    [UI((int) WinEnum.Win_Demo6_Test003, "Windows/UFlux/demo003/Window_FluxTest003")]
    public class Window_CustomLogicBind : AWindow<APropsDemo003Window>
    {
        public Window_CustomLogicBind(string path) : base(path)
        {
        }

        [ButtonOnclick("btn_ChangeChildNode")]
        private void btn_ChangeChildNode()
        {
            int stars = Random.Range(1, 6);
            this.Props.StarItems.Clear();
            for (int i = 0; i < stars; i++)
            {
                var item = new PropsDemo003Item();
                item.EquipmentIconPath = "Image/1";
                item.EquipmentName = "小新" + i + "号";
                item.SetAllPropertyChanged();
                this.Props.StarItems.Add(item);
            }

            this.Props.SetPropertyChange(nameof(this.Props.StarItems)); //设置属性更改
            this.CommitProps();
        }

        [ButtonOnclick("btn_ChangeOneNode")]
        private void btn_ChangeOneNode()
        {
            int i = Random.Range(1, 6);

            this.Props.OneNodeChange = new PropsDemo003Item();
            this.Props.OneNodeChange.EquipmentIconPath = "Image/" + i;
            this.Props.OneNodeChange.EquipmentName = "小新被刷新:" + i;
            this.Props.OneNodeChange.SetAllPropertyChanged();

            this.Props.SetPropertyChange(nameof(this.Props.OneNodeChange)); //设置属性更改

            this.CommitProps();
        }

        [ButtonOnclick("btn_ChangeNodeByInt")]
        private void btn_ChangeNodeByInt()
        {
            int count = Random.Range(1, 6);
            this.Props.value = count;
            this.Props.SetPropertyChange(nameof(this.Props.value)); //设置属性更改
            this.CommitProps();
        }

        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            this.Close();
        }
    }
}