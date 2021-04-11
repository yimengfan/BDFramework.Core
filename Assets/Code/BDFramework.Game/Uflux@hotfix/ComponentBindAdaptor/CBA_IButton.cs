using System;
using BDFramework.UFlux;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//这里的命名空间必须为：BDFramework.Uflux
namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是BDFrame的UI IButton适配器
    /// </summary>
    [ComponentBindAdaptor(typeof(IButton))]
    public class CBA_IButton : AComponentBindAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropComponentBindMap[nameof(IButton.onClick)] = SetProp_OnClick;
            setPropComponentBindMap[nameof(IButton.onClick.AddListener)] = SetProp_AddListener;
        }

        /// <summary>
        /// 设置回调
        /// 这里有点潜规则，Onclick代表替换，AddListener 代表注册增加监听
        /// </summary>
        /// <param name="value"></param>
      
        private void SetProp_OnClick(UIBehaviour uiBehaviour, object value)
        {
            var btn = uiBehaviour as IButton;
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
        private void SetProp_AddListener(UIBehaviour uiBehaviour, object value)
        {
            var btn = uiBehaviour as IButton;
            var action = value as Action;
            if (action != null)
            {
                //注册回调
                btn.onClick.AddListener(() => { action(); });
            }
        }
    }
}