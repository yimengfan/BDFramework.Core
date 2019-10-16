using System;
using System.Collections;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{

    public class TransformHelper
    {
        /// <summary>
        /// 控制几个子节点隐藏
        /// </summary>
        public  void ShowHideChildByNumber(){}

        
    }
    
    /// <summary>
    /// 自定义逻辑的适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(TransformHelper))]
    public class CustomLogicAdaptor_TransformHelper : AComponentAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropCustomAdaptorMap[nameof(TransformHelper.ShowHideChildByNumber)] = ShowHideChildByNumber;
          
        }
        /// <summary>
        /// 设置几个隐藏或者不隐藏
        /// </summary>
        /// <param name="value"></param>
        private void ShowHideChildByNumber(Transform transform,object value)
        {
            var count = (int) value;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(i<count);
            }
        }


        
    }
}