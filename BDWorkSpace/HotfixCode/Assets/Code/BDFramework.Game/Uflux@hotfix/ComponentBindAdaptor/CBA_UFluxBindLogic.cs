using System;
using System.Collections;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class UFluxBindLogic
    {
        
        /// <summary>
        /// 绑定单个子节点
        /// </summary>
        public void BindChild()
        {
        }

        /// <summary>
        /// 绑定所有子节点
        /// </summary>
        public void BindChildren()
        {

        }
    }

    /// <summary>
    /// 
    /// </summary>
    [ComponentBindAdaptor(typeof(UFluxBindLogic))]
    public class CBA_UFluxBindLogic : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropCustomLogicMap[nameof(UFluxBindLogic.BindChild)] = BindChild;
            setPropCustomLogicMap[nameof(UFluxBindLogic.BindChildren)] = BindChildren;
        }

        /// <summary>
        /// 绑定单个节点
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="value"></param>
        private void BindChild(Transform transform, object value)
        {
            var props = value as APropsBase;
            if (props == null)
            {
                Debug.LogError("类型不是props:" + value.GetType().Name);
                return;
            }

            UFlux.SetComponentProps(transform, props);
        }


        /// <summary>
        /// 绑定多个节点
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="value">Value必须为集合类型</param>
        private void BindChildren(Transform transform, object value)
        {
            IPropsList propsList = value as IPropsList;
            if (!propsList.IsChanged)
            {
                return;
            }
            
            //绑定子节点
            propsList.Foreach((idx,props) =>
            {
                if (idx < transform.childCount)
                {
                    UFlux.SetComponentProps(transform.GetChild(idx), props);
                }
            });


        }
    }
}