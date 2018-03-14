using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UI
{
    public class AViewBase : IView
    {
        public DataDrive_Service DataBinder { get; private set; }
        public Transform Transform { get; private set; }
        public void Show()
        {
            Transform.gameObject.SetActive(true);
        }

        public void Hide()
        {
            Transform.gameObject.SetActive(false);
        }

        public AViewBase(Transform t ,DataDrive_Service service)
        {
            this.DataBinder = service;
            this.Transform = t;
        }
        //
        virtual public void BindData()
        {
            
        }

        //
       virtual public void Destory()
        {
            //throw new System.NotImplementedException();
        }


        /// <summary>
        /// 根据数据结构自动填充Transform
        /// </summary>
        /// <param name="t"></param>
        /// <param name="o"></param>
        protected void AutoSetTranFormData(Transform t, object o)
        {
            var setList = new List<BMAutoSetValue>(t.GetComponentsInChildren<BMAutoSetValue>());
            
            var type = o.GetType();
            var fields =   type.GetFields();
            foreach (var f in fields)
            {
                //获取字段一致的节点 和属性名
                var trans = setList.Find(s => s.name == f.Name);
                var fAttr = f.GetCustomAttribute<BValueType>();
                if (trans!= null && fAttr != null)
                {
                    var component = trans.transform.GetComponent(fAttr.Type);
                    if (component != null)
                    {
                        if (fAttr.Type == typeof(Text))
                        {
                            var c = component as Text;
                            c.text = f.GetValue(o) as string;
                        }
                        else if (fAttr.Type == typeof(Image))
                        {
                            var c = component as Image;
                            var path = f.GetValue(o) as string;
                            //c.sprite =  ;
                        }
                        else if (fAttr.Type == typeof(Slider) || fAttr.Type == typeof(Scrollbar))
                        {

                        }
                        else
                        {
                            BDeBug.I.LogError("不支持类型,请扩展：" + f.Name  + "-"+ type.FullName);
                        }
                    }
                    else
                    {
                        BDeBug.I.LogError("字段标签错误：" + f.Name  + "-"+ type.FullName);
                    }
                }
                else
                {
                    BDeBug.I.LogError("无同名节点 或者无标签：" + f.Name  + "-" +type.FullName);
                }
            }
        }
    }
}