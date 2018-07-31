using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Core;
using BDFramework.UI;
using UnityEngine;
using UnityEngine.UI;

static public class UITools
{
    static private UITools_AutoSetTranformValueByData UITools_AutoSetTranformValueByData;
    static UITools()
    {
     
        UITools_AutoSetTranformValueByData = new UITools_AutoSetTranformValueByData();
    }

     
    #region 自动设置值

    /// <summary>
    /// 根据数据结构自动给Transform赋值
    /// </summary>
    /// <param name="t"></param>
    /// <param name="data"></param>
    static public void AutoSetValue(Transform t, object data)
    {
        UITools_AutoSetTranformValueByData.AutoSetValue(t,data);
    }
    

    #endregion
   
}
