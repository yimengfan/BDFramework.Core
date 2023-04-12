using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace HybridCLR.Editor.Installer
{
    public class InstallerWindow : EditorWindow
    {
        private InstallerController _controller;

        private void OnEnable()
        {
            _controller = new InstallerController();
        }

        private void OnGUI()
        {
            var rect = new Rect
            {
                x = EditorGUIUtility.currentViewWidth - 24,
                y = 5,
                width = 24,
                height = 24
            };
            var content = EditorGUIUtility.IconContent("Settings");
            content.tooltip = "点击打开HybridCLR Settings";
            if (GUI.Button(rect, content, GUI.skin.GetStyle("IconButton")))
            {
                SettingsService.OpenProjectSettings("Project/HybridCLR Settings");
            }

            bool hasInstall = _controller.HasInstalledHybridCLR();

            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"安装状态：{(hasInstall ? "已安装" : "未安装")}", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            EditorGUILayout.LabelField($"HybridCLR 版本:    {_controller.HybridclrLocalVersion}");
            GUILayout.Space(5f);
            EditorGUILayout.LabelField($"il2cpp_plus 版本:    {_controller.Il2cppPlusLocalVersion}");
            GUILayout.Space(5f);
            //GUIInstallButton("检查更新", "检查", UpdateHybridCLR);
            //GUILayout.Space(40f);
            
            GUILayout.Space(10f);


            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("待安装的 hybridclr 仓库版本号(或branch或tag)(默认取最新版本):", GUILayout.MaxWidth(400));
            //_hybridclrVersion = EditorGUILayout.TextField(_hybridclrVersion);
            //EditorGUILayout.EndHorizontal();

            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField($"待安装的 il2cpp_plus 仓库版本号(或branch或tag)(默认取{_controller.MajorVersion}-main分支最新版本):", GUILayout.MaxWidth(400));
            //_il2cppPlusVersion = EditorGUILayout.TextField(_il2cppPlusVersion);
            //EditorGUILayout.EndHorizontal();


            GUIInstallButton("安装hybridclr+il2cpp_plus代码到本地目录", "安装");
            EditorGUILayout.EndVertical();
        }

        private void GUIInstallButton(string content, string button)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            if (GUILayout.Button(button, GUILayout.Width(100)))
            {
                InstallLocalHybridCLR();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

        }

        private void InstallLocalHybridCLR()
        {
            _controller.InstallDefaultHybridCLR();
        }
    }
}
