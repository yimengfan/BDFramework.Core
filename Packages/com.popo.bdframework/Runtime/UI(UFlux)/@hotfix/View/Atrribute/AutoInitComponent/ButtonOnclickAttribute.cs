using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 自动初始化，按钮点击注册属性
    /// </summary>
    public class ButtonOnclickAttribute : AutoInitComponentAttribute
    {
        private string path;

        private bool isTriggerThisOnly = false;
        public ButtonOnclickAttribute(string path, bool isTriggerThisOnly = true)
        {
            this.path = path;
            this.isTriggerThisOnly = isTriggerThisOnly;
        }

        public override void AutoSetMethod(IComponent com, MethodInfo methodInfo)
        {
  
            Action action = null;


            var btn = com.Transform.Find(this.path)?.GetComponent<Button>();
            if (btn)
            {
                if (isTriggerThisOnly)
                {
                    btn.onClick.RemoveAllListeners();
                }
                btn.onClick.AddListener(() =>
                {
                    //触发按钮事件
                    methodInfo.Invoke(com, new object[] { });
                });
            }
            else
            {
                throw  new Exception("未找到Btn:" + this.path);
            }
        }
    }
}