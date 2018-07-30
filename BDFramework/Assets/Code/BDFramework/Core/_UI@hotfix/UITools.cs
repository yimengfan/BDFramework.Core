using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

static public class UITools 
{
    static UITools()
    {
     
        InitTypes();
    }


    #region 自动设置值

    static void InitTypes()
    {
        UIComponentFullname["Image"] = typeof(UnityEngine.UI.Image).FullName;
        UIComponentFullname["Text"] = typeof(UnityEngine.UI.Text).FullName;
        UIComponentFullname["Slider"] = typeof(UnityEngine.UI.Slider).FullName;
        UIComponentFullname["Scrollbar"] = typeof(UnityEngine.UI.Scrollbar).FullName;
        UIComponentFullname["Toggle"] = typeof(UnityEngine.UI.Toggle).FullName;
    }
     /// <summary>
    /// fullname缓存表
    /// </summary>
   static Dictionary<string,string> UIComponentFullname = new Dictionary<string, string>();
    /// <summary>
    /// 根据数据结构自动给Transform赋值
    /// </summary>
    /// <param name="t"></param>
    /// <param name="data"></param>
    static public void AutoSetValue(Transform t, object data)
    {
        var setList = new List<UITool_Attribute>(t.GetComponentsInChildren<UITool_Attribute>());

        var type = data.GetType();
        var fields = type.GetFields();
        foreach (var f in fields)
        {
            //获取字段一致的节点 和属性名
            var uiAttribute = setList.Find(s => s.AutoSetValueField == f.Name);
            //TODO: ILRuntime里面只能这样获取，而且属性里面存的type会有问题
            var attrs = f.GetCustomAttributes(typeof(UIComponentType), false);
            var fAttr = attrs.ToList().Find(a => a is UIComponentType) as UIComponentType;

            if (uiAttribute != null && fAttr != null && fAttr.ComponentName != null)
            {   
                if (fAttr.ComponentName == "Text")
                {
                    var c =   uiAttribute.transform.GetComponent<Text>();
                    c.text = f.GetValue(data).ToString();
                }
                else if (fAttr.ComponentName == "Image")
                {
                    var c =   uiAttribute.transform.GetComponent<Image>();
                    var path = f.GetValue(data).ToString();
                    c.sprite = BResources.Load<Sprite>(path);
                }
                else if (fAttr.ComponentName =="Slider" )
                {
                    var c =   uiAttribute.transform.GetComponent<Slider>();
                    var value = (float) f.GetValue(data);
                    c.value = value;
                }
                else if (fAttr.ComponentName == "Scrollbar")
                {
                    var c =   uiAttribute.transform.GetComponent<Scrollbar>();
                    var value = (float) f.GetValue(data);
                    c.value = value;
                }
                else if (fAttr.ComponentName ==  "Toggle")
                {
                    var c =   uiAttribute.transform.GetComponent<Toggle>();
                    var value = (bool) f.GetValue(data);
                    c.isOn = value;
                }
                //                     
                else
                {
                    BDebug.LogError("不支持类型,请扩展：" + f.Name + "-" + type.FullName);
                }
   
            }
            else
            {
                BDebug.LogError("无同名节点或者某些数据为null：" + f.Name +" - " +fAttr.ComponentName + " - " + type.FullName);
            }
        }
    }

    #endregion
   
}
