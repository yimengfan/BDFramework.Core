using UnityEngine;

namespace BDFramework.Editor.Tools
{
    static public class EditorGUIHelper
    {
        /// <summary>
        /// 1号大标题
        /// </summary>
        readonly static public GUIStyle LabelH1 = new GUIStyle()
        {
            fontSize = 30,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            }
        };

        /// <summary>
        /// 2号大标题
        /// </summary>
        readonly static public GUIStyle LabelH2 = new GUIStyle()
        {
            fontSize = 25,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            }
        };

        /// <summary>
        /// 3号大标题
        /// </summary>
        readonly static public GUIStyle LabelH3 = new GUIStyle()
        {
            fontSize = 20,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            }
        };

        /// <summary>
        /// 4号大标题
        /// </summary>
        readonly static public GUIStyle LabelH4 = new GUIStyle()
        {
            fontSize = 15,
            normal = new GUIStyleState()
            {
                textColor = Color.red
            }
        };

        /// <summary>
        /// 获取字体尺寸
        /// </summary>
        /// <param name="fontSize"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static GUIStyle GetFontStyle(Color color ,int fontSize = 10)
        {
            GUIStyle style = new GUIStyle()
            {
                fontSize = fontSize,
                normal = new GUIStyleState()
                {
                    textColor = color
                }
            };

            return style;
        }
}
}
