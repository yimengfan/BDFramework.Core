using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BDFramework.UI;
using ILRuntime.Runtime.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.Core
{
    public class UITools_AutoSetTranformValueByData
    {

        public UITools_AutoSetTranformValueByData()
        {
            IEnumeratorTool.StartCoroutine(this.IE_AutoClearCaheMap());
        }
        /// <summary>
        /// fullname缓存表
        /// </summary>
        Dictionary<int, object> UIComponentValue = new Dictionary<int, object>();

        Dictionary<Transform, Dictionary<string, ComponentValueCache>> ComponentCacheMap = new Dictionary<Transform, Dictionary<string, ComponentValueCache>>();

        /// <summary>
        /// 根据数据结构自动给Transform赋值
        /// </summary>
        /// <param name="t"></param>
        /// <param name="data"></param>
        public void AutoSetValue(Transform t, object data)
        {
            //建立缓存
            Dictionary<string, ComponentValueCache> coms = new Dictionary<string, ComponentValueCache>();
            bool isFirstFindComponent = false;
            if (ComponentCacheMap.TryGetValue(t, out coms) == false)
            {
                FirstFindComponent(t, data,out coms);

                this.ComponentCacheMap[t] = coms;
            }

            //开始赋值
            var fields = data.GetType().GetFields();
            foreach (var f in fields)
            {
                coms[f.Name].SetValue(f.GetValue(data));
            }

        }


        void FirstFindComponent(Transform t, object data, out Dictionary<string,ComponentValueCache> coms)
        {
             coms = new Dictionary<string, ComponentValueCache>();
             var setList = new List<UITool_Attribute>(t.GetComponentsInChildren<UITool_Attribute>());

            var type = data.GetType();
            var fields = type.GetFields();
            foreach (var f in fields)
            {
                //获取字段一致的节点 和属性名
                var trans = setList.Find(s => s.ToolTag_FieldName == f.Name);
                //TODO: ILRuntime里面只能这样获取，而且属性里面存的type会有问题
                var attrs = f.GetCustomAttributes(typeof(UIComponentType), false);
                var fAttr = attrs.ToList().Find(a => a is UIComponentType) as UIComponentType;

                if (trans != null && fAttr != null && fAttr.ComponentName != null)
                {
                    Component c = null;
                    if (fAttr.ComponentName == "Text")
                    {
                         c = trans.transform.GetComponent<Text>();
                    }
                    else if (fAttr.ComponentName == "Image")
                    {
                         c = trans.transform.GetComponent<Image>();
                    }
                    else if (fAttr.ComponentName == "Slider")
                    {
                         c = trans.transform.GetComponent<Slider>();
                    }
                    else if (fAttr.ComponentName == "Scrollbar")
                    {
                         c = trans.transform.GetComponent<Scrollbar>();
                    }
                    else if (fAttr.ComponentName == "Toggle")
                    {
                         c = trans.transform.GetComponent<Toggle>();
                    }
                    //                     
                    else
                    {
                        BDebug.LogError("不支持类型,请扩展：" + f.Name + "-" + type.FullName);
                    }
                    //存入
                    coms[f.Name] = new ComponentValueCache(c,fAttr.ComponentName);            
                }
                else
                {
                    BDebug.LogError("无同名节点或者某些数据为null：" + f.Name + " - " + fAttr.ComponentName + " - " + type.FullName);
                }
            }
        }
       

        /// <summary>
        /// 每隔一段时间 清理该表
        /// </summary>
        /// <returns></returns>
        IEnumerator IE_AutoClearCaheMap()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1);
                var keys = this.ComponentCacheMap.Keys.ToList();

                foreach (var k in keys)
                {
                    if (k == false)
                    {
                        this.ComponentCacheMap.Remove(k);
                    }
                }
            }
        }
    }


    public class ComponentValueCache
    {
        private Component component;
        private string type;
        private object value;

        public ComponentValueCache(Component c, string t)
        {
            this.component = c;
            this.type = t;
        }

        public void SetValue(object newObj)
        {
            if (type == "Text")
            {
                if (newObj!=null )
                {
                    if(value!= null && value.ToString() == newObj.ToString())return;        
                    var c = component as Text;
                    c.text = newObj.ToString();

                    this.value = newObj;
                }            
            }
            else if (type == "Image")
            {
                if (newObj!=null )
                {
                    if(value!= null && value.ToString() == newObj.ToString())return;    
                    var c =  component as Image;
                    c.sprite = BResources.Load<Sprite>(newObj.ToString());
                    this.value = newObj;
                }
                
            }
            else if (type == "Slider")
            {
                if (newObj != null)
                {
                    if(value!= null && (float)value == (float)newObj)return;    
                    var c = component as Slider;
                    c.value =  (float)newObj;
                    this.value = newObj;
                }
            }
            else if (type == "Scrollbar")
            {
                if (newObj != null )
                {
                    if(value!= null && (float)value == (float)newObj)return;    
                    var c = component as Scrollbar;
                    c.value =  (float)newObj;
                    this.value = newObj;
                }
            }
            else if (type == "Toggle")
            {
                if (newObj != null )
                {
                    if(value!= null && (bool)value == (bool)newObj)return;    
                    var c = component as Toggle;
                    c.isOn =  (bool)newObj;
                    this.value = newObj;
                }
            }
            
        }
    }
}