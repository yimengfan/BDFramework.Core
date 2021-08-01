using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using BDFramework.UI;
using UnityEngine;

namespace Game.demo6_UFlux.ComponentBindAdaptor
{
    public class Props_Window : APropsBase
    {

        /// <summary>
        /// 自定义属性：值转换成执行逻辑
        /// CBA_TransformHelper  就是处理这里的业务 将int值执行为显示、隐藏子节点数
        /// 这里的业务设计的不是很符合实际需求，请勿照搬
        /// 只是为了展示怎么实现自定义逻辑
        /// 如Img加载、Text 都是这样实现的
        /// </summary>
        [ComponentValueBind("Items",typeof(TransformHelper), nameof(TransformHelper.ShowHideChildByNumber))]
        public int ShowChildNum = 5;
    }

    [UI((int) WinEnum.Win_UFlux_Test003, "Windows/UFlux/02CustomComponentBindAdator/02/Window_Test")]
    public class Window_CustomLogicBind : AWindow<Props_Window>
    {
        public Window_CustomLogicBind(string path) : base(path)
        {
        }
        

        [ButtonOnclick("btn_IntValueToLogic")]
        private void btn_ChangeNodeByInt()
        {

            
            Debug.Log("自定义值映射，点击跟踪代码");
            this.Props.ShowChildNum = Random.Range(1, 6);
            this.CommitProps();
        }

        [ButtonOnclick("btn_Close")]
        private void btn_Close()
        {
            this.Close();
        }
    }
}