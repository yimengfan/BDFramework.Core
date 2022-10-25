using System;
using System.Collections;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class TransformHelper
    {
        /// <summary>
        /// 控制子节点显示隐藏
        /// </summary>
        public void ShowHideChildByNumber() { }
    }

    /// <summary>
    /// 节点适配器
    /// </summary>
    [ComponentBindAdaptor(typeof(TransformHelper))]
    public class CBA_TransformHelper : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropCustomLogicMap[nameof(TransformHelper.ShowHideChildByNumber)] = ShowHideChildByNumber;
        }

        /// <summary>
        /// 设置几个隐藏或者不隐藏
        /// </summary>
        public void ShowHideChildByNumber(Transform transform, object value)
        {
            var count = (int) value;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(i<count);
            }
        }
    }
}