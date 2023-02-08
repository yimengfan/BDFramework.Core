using BDFramework.Core.Tools;
using BDFramework.Editor.Unity3dEx;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Tools.RuntimeEditor
{
#if UNITY_EDITOR

    /// <summary>
    /// Bdebug的编辑器
    /// </summary>
    [CustomEditor(typeof(BDebug))]
    public class BDebugEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var debug = target as BDebug;

            //log
            debug.IsLog = EditorGUILayout.Toggle("EnableLog", debug.IsLog);
            if (!Application.isPlaying)
            {
                if (debug.IsLog)
                {
                    EnableDebug();
                }
                else
                {
                    DisableDebug();
                }
            }

            //开启log与否
            debug.LogTagList.Sort((a, b) =>
            {
                //用tag排序
                return string.Compare(a.Tag, b.Tag);
            });
            //
            GUILayout.Label("Tag num:" + debug.LogTagList.Count);
            foreach (var tag in debug.LogTagList)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Tag: " + tag.Tag, GUILayout.Width(200));

                    tag.IsLog = EditorGUILayout.Toggle(tag.IsLog);
                }
                GUILayout.EndHorizontal();
            }
        }


        /// <summary>
        /// 打开debug
        /// 此操作在打包前进行调用，管理ENABLE_BDEBUG宏
        /// </summary>
        static public void EnableDebug()
        {
            
            Unity3dEditorEx.AddSymbols(BDebug.ENABLE_BDEBUG);
            Unity3dEditorEx.AddSymbols("DEBUG");
        }


        /// <summary>
        /// 关闭debug
        /// 此操作在打包前进行调用，管理ENABLE_BDEBUG宏
        /// </summary>
        static public void DisableDebug()
        {
            Unity3dEditorEx.RemoveSymbols(BDebug.ENABLE_BDEBUG);
            Unity3dEditorEx.RemoveSymbols("DEBUG");
        }
    }

#endif
}