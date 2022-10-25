using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Button适配器
    /// </summary>
    [ComponentBindAdaptor(typeof(Toggle))]
    public class CBA_Toggle : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            setPropComponentBindMap[nameof(Toggle.group)] = SetProp_Group;
            setPropComponentBindMap[nameof(Toggle.onValueChanged)] = SetProp_OnValueChaged;
            setPropComponentBindMap[nameof(Toggle.interactable)] = SetProp_Interactable;
            setPropComponentBindMap[nameof(Toggle.isOn)] = SetProp_IsOn;
        }
        /// <summary>
        /// 设置是否可交互
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="value"></param>
        private void SetProp_Interactable(UIBehaviour ui, object value)
        {
            var btn = (Toggle) ui;
            if (value is bool interactable)
            {
                btn.interactable = interactable;
            }
        }

        /// <summary>
        ///  设置选中
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="value"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SetProp_IsOn(UIBehaviour ui, object value)
        {
            var toggle = ui as Toggle;
            bool isOn = (bool)value;
            toggle.isOn = isOn;
        }

        /// <summary>
        /// 设置toggle事件
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="value"></param>
        private void SetProp_OnValueChaged(UIBehaviour ui, object value)
        {
            var toggle = ui as Toggle;
            var action = value as Action<bool>;
            if (action!=null)
            {
                toggle.onValueChanged.AddListener((isOn) => action(isOn));
            }
        }

        /// <summary>
        /// 设置toggl组
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="value"></param>
        private void SetProp_Group(UIBehaviour ui, object value)
        {
            var toggle = ui as Toggle;
            toggle.group = ((Transform)value).GetComponent<ToggleGroup>();
        }
    }
}