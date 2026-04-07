using System;
using System.IO;
using BDFramework.Logs;
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
            if (debug == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PlayerLog", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Editor 下 `BDebug` 只负责 Console 输出，日志序列化由 `Editor_UnityLogHook` 劫持；Player/真机下由 `BDebug` 自己的二进制日志序列化强制开启。加密可配置，`playerlogs/` 默认最多保留 20 份。", MessageType.Info);
            debug.EnablePlayerLogEncryption = EditorGUILayout.Toggle("Enable Encrypt", debug.EnablePlayerLogEncryption);
            debug.PlayerLogEncryptPassword = EditorGUILayout.PasswordField("Encrypt Password", debug.PlayerLogEncryptPassword);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("选择目录并批量解密 PlayerLog"))
                {
                    DecryptPlayerLogDirectory(debug);
                }
            }

            //开启log与否
            debug.DisableLogTagList.Sort((a, b) =>
            {
                //用tag排序
                return string.Compare(a.Tag, b.Tag, StringComparison.Ordinal);
            });
            //
            GUILayout.Label("Tag num:" + debug.DisableLogTagList.Count);
            foreach (var tag in debug.DisableLogTagList)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Tag: " + tag.Tag, GUILayout.Width(200));

                    tag.IsLog = EditorGUILayout.Toggle(tag.IsLog);
                }
                GUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(debug);
            }
        }

        private static void DecryptPlayerLogDirectory(BDebug debug)
        {
            var directory = EditorUtility.OpenFolderPanel("选择包含 playerlog_*.bin 的目录", BDebug.PlayerLogRootPath, string.Empty);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var files = Directory.GetFiles(directory, "playerlog_*.bin", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("批量解密", "所选目录下没有找到 playerlog_*.bin 文件。", "确定");
                return;
            }

            var password = string.IsNullOrEmpty(debug.PlayerLogEncryptPassword)
                ? LogCrypto.DEFAULT_PASSWORD
                : debug.PlayerLogEncryptPassword;
            var successCount = 0;
            var failedFiles = string.Empty;
            foreach (var file in files)
            {
                try
                {
                    LogReader.ExportToText(file, null, password);
                    successCount++;
                }
                catch (Exception e)
                {
                    failedFiles += $"- {Path.GetFileName(file)}: {e.Message}\n";
                }
            }

            if (string.IsNullOrEmpty(failedFiles))
            {
                EditorUtility.DisplayDialog("批量解密", $"已成功解密 {successCount} 个文件，输出到原目录。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("批量解密", $"成功: {successCount}\n失败:\n{failedFiles}", "确定");
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