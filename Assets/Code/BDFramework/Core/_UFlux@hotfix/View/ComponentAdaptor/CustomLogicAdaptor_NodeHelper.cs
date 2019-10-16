using System;
using System.Collections;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class UFluxAutoLogic
    {
        //设置单个节点赋值
        public void SetChildValue()
        {
        }

        /// <summary>
        /// foreach 设置子节点value
        /// </summary>
        public void ForeahSetChildValue()
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(UFluxAutoLogic))]
    public class CustomLogicAdaptor_NodeHelper : AComponentAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropCustomAdaptorMap[nameof(UFluxAutoLogic.SetChildValue)]       = SetChildValue;
            setPropCustomAdaptorMap[nameof(UFluxAutoLogic.ForeahSetChildValue)] = ForeahSetChildValueFormArray;
        }


        /// <summary>
        /// 设置属性
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="value"></param>
        private void SetChildValue(Transform transform, object value)
        {
            var props = value as PropsBase;
            UFlux.SetComponentValue(transform, props);
        }


        /// <summary>
        /// 将List中的数据设置给子节点
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="value">Value必须为集合类型</param>
        private void ForeahSetChildValueFormArray(Transform transform, object value)
        {
            ICollection Collection = value as ICollection;
            if (Collection == null) return;
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
                        UFlux.SetComponentValue(child, props);
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
            for (int i = count - 1; i < transform.childCount; i++)
            {
                transform.GetChild(count).gameObject.SetActive(false);
            }
        }
    }
}