using UnityEngine;


public class UITool_Attribute : MonoBehaviour
{

    /// <summary>
    /// 用来生成代码字段
    /// </summary>
    public bool GenAttribute_TranformPath = false;
    /// <summary>
    /// 用来绑定 数据回调
    /// </summary>
    public string GenAttribute_BindData;
    /// <summary>
    /// 用来自动赋值的字段  
    /// </summary>
    public string ToolTag_FieldName = "";
}
