using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.UI
{

    public class EditorWindows_CreateUI : EditorWindow
    {

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            OnGUI_WindowSelect();
            OnGUI_EditorTransform();
            OnGUI_EditorButton();
            GUILayout.EndHorizontal();
            
        }


        Vector2  windowSelectPosition = Vector2.zero;
        private void OnGUI_WindowSelect()
        {
            //开始滚动列表
            windowSelectPosition = GUILayout.BeginScrollView(windowSelectPosition, GUILayout.Width(350) ,GUILayout.Height(300));
            //竖列排版
            {
                GUILayout.BeginVertical();

                for (int i = 0; i < 15; i++)
                {
                    if (GUILayout.Button("testBtton" + i , GUILayout.Width(300)))
                    {
                        Debug.Log(i);
                    }
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }


        private void OnGUI_EditorTransform()
        {
            
        }


        private void OnGUI_EditorButton()
        {
            
        }
    }
}