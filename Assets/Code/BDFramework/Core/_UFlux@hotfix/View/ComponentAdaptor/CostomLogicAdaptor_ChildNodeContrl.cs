using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{

    public class ChildNodeControl
    {
        /// <summary>
        /// 控制几个子节点隐藏
        /// </summary>
        public  void ShowChildCount(){}
    }
    
    /// <summary>
    /// 这里是UnityEngine的UI Text适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(ChildNodeControl))]
    public class CostomLogicAdaptor_ChildNodeContrl : AComponentAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropCustomActionMap[nameof(ChildNodeControl.ShowChildCount)] = SetProp_Text;
        }
        /// <summary>
        /// 设置几个隐藏或者不隐藏
        /// </summary>
        /// <param name="value"></param>
        private void SetProp_Text(Transform transform,object value)
        {
            var count = (int) value;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(i<count);
            }
            
        }
    }
}