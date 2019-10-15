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

        /// <summary>
        /// foreach 设置子节点value
        /// </summary>
        public void ForeahSetChildValueFormArray(){}
    }
    
    /// <summary>
    /// 这里是UnityEngine的UI Text适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(TransformHelper))]
    public class CustomLogicAdaptor_TransformHelper : AComponentAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropCustomAdaptorMap[nameof(TransformHelper.ShowHideChildByNumber)] = ShowHideChildByNumber;
            setPropCustomAdaptorMap[nameof(TransformHelper.ForeahSetChildValueFormArray)] = ForeahSetChildValueFormArray;
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


       /// <summary>
       /// 将List中的数据设置给子节点
       /// </summary>
       /// <param name="transform"></param>
       /// <param name="value">Value必须为集合类型</param>
        private void ForeahSetChildValueFormArray(Transform transform,object value)
        {
            ICollection Collection = value  as ICollection;
            if(Collection==null) return;
            int count = 0;
            foreach (var item in Collection)
            {
                var props = item as PropsBase;
                if (props != null)
                {
                    transform.gameObject.SetActive(true);
                    var child = transform.GetChild(count);
                    if (child)
                    {
                        UFlux.SetComponentValue(child,props);
                    }
                }
                else
                {
                    BDebug.LogError("list数据错误:" + value.GetType());
                    return;
                }
                count++;
            }

            //数据不够的隐藏
            for (int i = count-1; i < transform.childCount; i++)
            {
                transform.GetChild(count).gameObject.SetActive(false);
            }

        }
    }
}