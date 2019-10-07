using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Button适配器
    /// </summary>
    [ComponentAdaptorProcessAttribute(typeof(Button))]
    public class ComponentAdaptor_Button : AComponentAdaptor
    {
        /// <summary>
        /// 设置回调
        /// 这里有点潜规则，Onclick代表替换，AddListener 代表注册增加监听
        /// </summary>
        /// <param name="value"></param>
        [ComponentValueAdaptor(nameof(Button.onClick))]
        private void SetProp_OnClick(UIBehaviour uiBehaviour, object value)
        {
            var btn = uiBehaviour as Button;
            var action = value as Action;
            btn.onClick.RemoveAllListeners();
            if (action != null)
            {
                //注册回调
                btn.onClick.AddListener(() => { action(); });
            }
        }
        
        /// <summary>
        /// 设置回调
        /// 这里有点潜规则，Onclick代表替换，AddListener 代表注册增加监听 
        /// </summary>
        /// <param name="value"></param>
        [ComponentValueAdaptor(nameof(Button.onClick.AddListener))]
        private void SetProp_AddListener(UIBehaviour uiBehaviour, object value)
        {
            var btn = uiBehaviour as Button;
            var action = value as Action;
            if (action != null)
            {
                //注册回调
                btn.onClick.AddListener(() => { action(); });
            }
        }
    }
}