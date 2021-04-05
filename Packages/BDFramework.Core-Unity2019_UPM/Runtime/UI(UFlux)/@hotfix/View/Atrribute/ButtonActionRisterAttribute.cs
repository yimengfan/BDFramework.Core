using System;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    public class ButtonActionRisterAttribute : UFluxAttribute
    {
        private string path;

        public ButtonActionRisterAttribute(string path)
        {
            this.path = path;
        }

        public override void Do(Transform root, object fieldValue)
        {
            base.Do(root, fieldValue);

            var action = fieldValue as Action;
            if (action == null)
            {
                BDebug.LogError("字段类型错误,必须为Action");
                return;
            }

            var btn = root.Find(this.path)?.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.AddListener(() => { action.Invoke(); });
            }
            else
            {
                BDebug.LogError("未找到Btn:" + root);
            }
        }
    }
}