using System;
using BDFramework.ScreenView;
using BDFramework.UFlux;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//这里的命名空间必须为：BDFramework.Uflux
namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Button适配器
    /// </summary>
    [ComponentBindAdaptor(typeof(Button))]
    public class CBA_Button : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            
            setPropComponentBindMap[nameof(Button.onClick)] = SetProp_OnClick;
            setPropComponentBindMap[nameof(Button.onClick.AddListener)] = SetProp_AddListener;
        }

        /// <summary>
        /// 设置回调
        /// 这里有点潜规则，Onclick代表替换，AddListener 代表注册增加监听
        /// </summary>
        /// <param name="value"></param>
      
        private void SetProp_OnClick(UIBehaviour uiBehaviour, object value)
        {
            var btn = uiBehaviour as Button;
            var action = value as Action;
            btn.onClick.RemoveAllListeners();
            if (action != null)
            {
                //注册回调
                btn.onClick.RemoveAllListeners();
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
            var btn = uiBehaviour as Button;
            var action = value as Action;
            if (action != null)
            {
                //注册回调
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => { action(); });
            }
        }
    }
}