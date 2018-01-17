using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Widget_ChatBox)),CanEditMultipleObjects]
public class WidgetEditor_Chatbox : Editor
{
    Widget_ChatBox widget;
    private void OnEnable()
    {
        widget = this.target as Widget_ChatBox;
        widget.Init();
        widget.ResizeBoxSize();
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        widget.ResizeBoxSize();
       
    }
}
