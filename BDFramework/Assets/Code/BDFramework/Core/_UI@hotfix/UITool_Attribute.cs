using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITool_Attribute : MonoBehaviour
{

    /// <summary>
    /// 用来生成代码字段
    /// </summary>
    public bool IsAutoSetTransform = false;
    /// <summary>
    /// 用来绑定 数据回调
    /// </summary>
    public string AutoBindModelData;
    /// <summary>
    /// 用来自动赋值的字段
    /// </summary>
    public string AutoSetValueField = "";
}
