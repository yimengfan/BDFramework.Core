using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Unity3dEx
{
    /// <summary>
    /// guilayout的拓展
    /// </summary>
    public class EditorGUILayoutEx
    {
        /// <summary>
        /// 画横线
        /// </summary>
        /// <param name="color"></param>
        /// <param name="height"></param>
        public static void Layout_DrawLineH(Color color, float height = 4f)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(height);
        }

        /// <summary>
        /// 画竖线
        /// </summary>
        /// <param name="color"></param>
        /// <param name="width"></param>
        public static void Layout_DrawLineV(Color color, float width = 4f)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, width, rect.height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(width);
        }
    }
}