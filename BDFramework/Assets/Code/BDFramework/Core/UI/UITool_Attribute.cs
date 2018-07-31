using System;
using System.Collections;
using System.Collections.Generic;
using Code.Game.Windows;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITool_Attribute : MonoBehaviour
{

    /// <summary>
    /// 用来生成代码字段
    /// </summary>
    public bool GenAttibute_TranformPath = false;
    /// <summary>
    /// 用来绑定 数据回调
    /// </summary>
    public string GenAttitude_BindData;
    /// <summary>
    /// 用来自动赋值的字段
    /// </summary>
    public string ClassFieldName = "";
}
