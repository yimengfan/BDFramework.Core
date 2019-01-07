using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Web;
using BDFramework.UI;
using ILRuntime.Runtime.Generated;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BDFramework.ResourceMgr;
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

        Dictionary<Transform, Dictionary<string, ComponentValueCache>> ComponentCacheMap =
            new Dictionary<Transform, Dictionary<string, ComponentValueCache>>();

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
                coms = FirstFindComponent(t, data);
                this.ComponentCacheMap[t] = coms;
            }

            //开始赋值
            var fields = data.GetType().GetFields();
            foreach (var f in fields)
            {
                ComponentValueCache cc = null;
                if (coms.TryGetValue(f.Name, out cc))
                {
                    cc.SetValue(f.GetValue(data));
                }
                else
                {
                    BDebug.Log("该字段配置无节点:" + f.Name);
                }
             
            }
        }


        Dictionary<string, ComponentValueCache> FirstFindComponent(Transform t, object data)
        {
            Dictionary<string, ComponentValueCache> coms = new Dictionary<string, ComponentValueCache>();
            var setList = new List<UITool_Attribute>(t.GetComponentsInChildren<UITool_Attribute>());

            var type = data.GetType();
            var fields = type.GetFields();
            foreach (var f in fields)
            {
             
                //TODO: ILRuntime里面只能这样获取，而且属性里面存的type会有问题
                var attrs = f.GetCustomAttributes(typeof(ComponentAttribute), false);
                var fAttr = attrs.ToList().Find(a => a is ComponentAttribute) as ComponentAttribute;
               
                //
                if (fAttr != null && fAttr.ComponentType != null)
                {
                    //获取uitools name一致的节点
                    var trans = setList.Find(s => s.ToolTag_FieldName == fAttr.ToolTag_FieldName);
                    if (trans != null)
                    {
                        //存入
                        coms[f.Name] = new ComponentValueCache(fAttr,trans.transform);
                    }
                    
                }
                else
                {
                    BDebug.LogError("无同名节点或者某些数据为null：" + f.Name + " - " + fAttr.ComponentType + " - " + type.FullName);
                }
            }

            return coms;
        }


        /// <summary>
        /// 每隔一段时间 清理该表
        /// </summary>
        /// <returns></returns>
        IEnumerator IE_AutoClearCaheMap()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(30);
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
        private UIBehaviour component;
        private object value;
        private ComponentAttribute componentAttr;
        Dictionary<ComponentType,Type> typesMap = new Dictionary<ComponentType, Type>();
        public ComponentValueCache(ComponentAttribute componentAttr,Transform transform)
        {
          
            this.componentAttr = componentAttr;
            typesMap[ComponentType.Image] = typeof(Image);
            typesMap[ComponentType.Text] = typeof(Text);
            typesMap[ComponentType.Slider] = typeof(Slider);
            typesMap[ComponentType.ScrollBar] = typeof(Scrollbar);
            typesMap[ComponentType.Toggle] = typeof(Toggle);
            var c = transform.GetComponent(typesMap[componentAttr.ComponentType]);
            this.component = c as UIBehaviour;
        }

        public void SetValue( object newObj)
        {
            if (newObj ==null) return;
           
            if (componentAttr.CustomField != CustomField.Null) //先处理自定义的值
            {
                switch (componentAttr.CustomField)
                {
                    case CustomField.ResourcePath:
                        if (componentAttr.ComponentType == ComponentType.Image)
                        {
                            if (value != null && value.ToString() == newObj.ToString()) 
                                break;
                            var c = component as Image;
                            c.sprite = BResources.Load<Sprite>(newObj.ToString());
                            this.value = newObj;
                        }
                        break;
                    case CustomField.GameObjectActive:
                    {
                        component.gameObject.SetActive((bool) newObj);
                        this.value = newObj;
                    }
                        break;
                    case CustomField.ComponentEnable:
                    {
                        component.enabled = ((bool) newObj);
                        this.value = newObj;
                    }
                    break;
                }
            }
            else
            {            
                SetComponentValue(typesMap[componentAttr.ComponentType],componentAttr.ComponentField, this.component,newObj);
            }
        }

        /// <summary>
        /// 设置组件的值
        /// </summary>
        /// <param name="field"></param>
        /// <param name="behavior"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        private void SetComponentValue(Type t , string field, UIBehaviour behavior, object value)
        {
            MemberInfo info = null;
            info  = t.GetField(field);
            if (info == null)
            {
                info = t.GetProperty(field);
            }

            if (info == null)
            {
                BDebug.Log("不存在字段||属性：" + field);
                return;
            }

            if (info.MemberType  == MemberTypes.Field)
            {
                ((FieldInfo)info).SetValue(behavior,value);
            }
            else if(info.MemberType == MemberTypes.Property)
            {
                ((PropertyInfo)info).SetValue(behavior,value);
            }
        }


        /// <summary>
        /// 比较值
        /// </summary>
        /// <param name="newobjet"></param>
        /// <returns></returns>
        private bool CompareValue(object newobjet)
        {
            if (this.value == null)
            {
                return false;
            }
            else if (this.value is double && newobjet is double)
            {
                return (double) value == (double) newobjet;
            }
            else if (this.value is float && newobjet is float)
            {
                return (float) value == (float) newobjet;
            }
            else if (this.value is int && newobjet is int)
            {
                return (int) value == (int) newobjet;
            }
            else if (this.value is string && newobjet is string)
            {
                return (string) value == (string) newobjet;
            }
            else if (this.value is bool && newobjet is bool)
            {
                return (bool) value == (bool) newobjet;
            }

            return false;
        }
        
    }
}