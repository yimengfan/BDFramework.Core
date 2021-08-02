using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 组件绑定基类
    /// </summary>
    abstract public class AComponentBindAdaptor
    {
        public delegate void SetUIBehaviourDelegate(UIBehaviour ui, object value);
        public  delegate void SetTransformDelegate(Transform transform, object value);
        /// <summary>
        /// 组件绑定map
        /// </summary>
        protected Dictionary<string, SetUIBehaviourDelegate> setPropComponentBindMap = new Dictionary<string, SetUIBehaviourDelegate>();
        /// <summary>
        /// 自定义逻辑map
        /// </summary>
        protected Dictionary<string, SetTransformDelegate> setPropCustomLogicMap = new Dictionary<string, SetTransformDelegate>();

        public AComponentBindAdaptor()
        {
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init()
        {
            setPropComponentBindMap[nameof(UIBehaviour.enabled)] = SetProp_Enable;
            setPropComponentBindMap[nameof(UIBehaviour.gameObject.active)] = SetProp_Active;
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="uiBehaviour"></param>
        /// <param name="propName"></param>
        /// <param name="propValue"></param>
        public virtual void SetData(UIBehaviour uiBehaviour, string propName, object propValue)
        {
            SetUIBehaviourDelegate action = null;
            this.setPropComponentBindMap.TryGetValue(propName, out action);
            if (action != null)
            {
                action(uiBehaviour, propValue);
            }
            else
            {
                BDebug.LogError("不存在赋值字段:" + propName);
            }
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="propName"></param>
        /// <param name="propValue"></param>
        public virtual void SetData(Transform transform, string propName, object propValue)
        {
            SetTransformDelegate action = null;
            this.setPropCustomLogicMap.TryGetValue(propName, out action);
            if (action != null)
            {
                action(transform, propValue);
            }
            else
            {
                BDebug.LogError("不存在赋值字段:" + propName);
            }
        }

        /// <summary>
        /// enable
        /// </summary>
        /// <param name="propValue"></param>
        private void SetProp_Enable(UIBehaviour uiBehaviour, object propValue)
        {
            uiBehaviour.enabled = (bool) propValue;
        }

        /// <summary>
        /// active
        /// </summary>
        /// <param name="propValue"></param>
        private void SetProp_Active(UIBehaviour uiBehaviour, object propValue)
        {
            uiBehaviour.gameObject.SetActive((bool) propValue);
        }
    }
}