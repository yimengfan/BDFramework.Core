using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using BDFramework;
namespace BDFramework.UI
{
    public class AViewBase :AIEnumeratorTaskMgr,IView
    {
        public DataDriven_Service Model { get; private set; }
        public Transform Transform { get; private set; }
        public void Show()
        {
            Transform.gameObject.SetActive(true);
        }

        public void Hide()
        {
            Transform.gameObject.SetActive(false);
        }

        public AViewBase(Transform t ,DataDriven_Service service)
        {
            this.Model = service;
            this.Transform = t;
        }
        //
        virtual public void BindModel()
        {
            
        }

        //
       virtual public void Destory()
        {
            //throw new System.NotImplementedException();
        }


        /// <summary>
        /// 根据数据结构自动给Transform赋值
        /// </summary>
        /// <param name="t"></param>
        /// <param name="o"></param>
        protected void AutoSetTranFormData(Transform t, object o)
        {
            var setList = new List<AutoSetValue>(t.GetComponentsInChildren<AutoSetValue>());
            
            var type = o.GetType();
            var fields =   type.GetFields();
            foreach (var f in fields)
            {
                //获取字段一致的节点 和属性名
                var trans = setList.Find(s => s.name == f.Name);
                var fAttr = f.GetCustomAttribute<ValueType>();
                if (trans!= null && fAttr != null)
                {
                    var component = trans.transform.GetComponent(fAttr.Type);
                    if (component != null)
                    {
                        if (fAttr.Type == typeof(Text))
                        {
                            var c = component as Text;
                            c.text = f.GetValue(o).ToString();
                        }
                        else if (fAttr.Type == typeof(Image))
                        {
                            var c     = component as Image;
                            var path  = f.GetValue(o).ToString();                           
                            c.sprite  = BResources.Load<Sprite>(path);
                        }
                        else if (fAttr.Type == typeof(Slider))
                        {
                            var c      = component as Slider;
                            var value  = (float)f.GetValue(o);
                            c.value = value;
                        }
                        else if (fAttr.Type == typeof(Scrollbar))
                        {
                            var c      = component as Scrollbar;
                            var value  = (float)f.GetValue(o);
                            c.value = value;
                        }
                        else if (fAttr.Type == typeof(Toggle))
                        {
                            var c      = component as Toggle;
                            var value  = (bool)f.GetValue(o);
                            c.isOn = value;
                        }
                        //                     
                        else
                        {
                            BDebug.LogError("不支持类型,请扩展：" + f.Name  + "-"+ type.FullName);
                        }
                    }
                    else
                    {
                        BDebug.LogError("字段标签错误：" + f.Name  + "-"+ type.FullName);
                    }
                }
                else
                {
                    BDebug.LogError("无同名节点 或者无标签：" + f.Name  + "-" +type.FullName);
                }
            }
        }
    }
}