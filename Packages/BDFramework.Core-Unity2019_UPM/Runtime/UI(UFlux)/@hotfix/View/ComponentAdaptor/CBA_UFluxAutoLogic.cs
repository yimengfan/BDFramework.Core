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
        public void ForeachSetChildValue()
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [ComponentBindAdaptor(nameof(UFluxAutoLogic))]
    public class CBA_UFluxAutoLogic : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropCustomLogicMap[nameof(UFluxAutoLogic.SetChildValue)] = SetChildValue;
            setPropCustomLogicMap[nameof(UFluxAutoLogic.ForeachSetChildValue)] = ForeahSetChildValue;
        }

        /// <summary>
        /// 设置属性
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="value"></param>
        private void SetChildValue(Transform transform, object value)
        {
            var props = value as PropsBase;
            if (props == null)
            {
                Debug.LogError("类型不是props:" + value.GetType().Name);
                return;
            }

            UFlux.SetComponentValue(transform, props);
        }


        /// <summary>
        /// 将List中的数据设置给子节点
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="value">Value必须为集合类型</param>
        private void ForeahSetChildValue(Transform transform, object value)
        {
            ICollection Collection = value as ICollection;
            if (Collection == null)
                return;
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

            for (int i = 0; i < transform.childCount; i++)
            {
                if (i > count - 1)
                    transform.GetChild(i).gameObject.SetActive(false);
                else
                    transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
}