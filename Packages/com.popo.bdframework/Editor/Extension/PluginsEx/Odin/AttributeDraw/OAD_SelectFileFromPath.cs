using System.IO;
using System.Linq;
using BDFramework.Editor.Unity3dEx.PluginsEx.Odin.Attribute;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
namespace BDFramework.Editor.Unity3dEx.PluginsEx.Odin.AttributeDraw
{
    using Sirenix.OdinInspector;
    using Sirenix.OdinInspector.Editor;
    /// <summary>
    /// Odin拓展，下拉列表选择文件
    /// </summary>
    public class OAD_SelectFileFromPath : OdinAttributeDrawer<Ex_SelectFileFromPath, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            GUILayout.BeginHorizontal();
            {
                float labelwidth = 0;
                var labeltext = this.Property.GetAttribute<LabelWidthAttribute>();
                if (labeltext != null)
                {
                    labelwidth = labeltext.Width;
                }

                if (labelwidth == 0)
                {
                    var attr = this.Property.GetAttribute<HorizontalGroupAttribute>();
                    if (attr != null)
                    {
                        labelwidth = attr.LabelWidth;
                    }
                }

                if (labelwidth == 0)
                {
                    GUILayout.Label(label);
                }
                else
                {
                    GUILayout.Label(label, GUILayout.Width(labelwidth));
                }

                var fileList = Directory.GetFiles(this.Attribute.Path, this.Attribute.SearchPartten, SearchOption.AllDirectories).Select((f) => IPath.ReplaceBackSlash(f)).ToList();
                if (fileList.Count == 0)
                {
                    fileList.Add("无文件");
                }

                var selects = fileList.Select((f) => Path.GetFileName(f)).ToList();

                //寻找index
                var idx = selects.FindIndex((fp) => Path.GetFileName(this.ValueEntry.SmartValue) == fp);
                if (idx == -1)
                {
                    idx = 0;
                }

                var newIdx = EditorGUILayout.Popup(idx, selects.ToArray(), GUILayout.Width(this.Attribute.Width));
                if (newIdx >= 0 && newIdx < selects.Count)
                {
                    //赋值返回
                    this.ValueEntry.SmartValue = fileList[newIdx];
                    if (idx != newIdx)
                    {
                        Debug.Log("选择：" + this.ValueEntry.SmartValue);
                    }
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }
    }
}
#endif
