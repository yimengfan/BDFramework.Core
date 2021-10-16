using System;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.PublishPipeline
{
    /// <summary>
    /// 一键构建资源
    /// </summary>
    public class EditorWindow_CICD : EditorWindow
    {

        [MenuItem("BDFrameWork工具箱/3.CI、CD", false, (int)BDEditorGlobalMenuItemOrderEnum.BuildPipeline_CICD)]
        public static void Open()
        {
            var window =  EditorWindow.GetWindow<EditorWindow_CICD>( false, "CI、CD");
            window.Show();
            window.Focus();
        }


        private void OnGUI()
        {
            OnGUI_BuildpipelineCI();
        }
        private void OnDisable()
        {
            //保存
            BDEditorApplication.BDFrameWorkFrameEditorSetting.Save();
        }

        /// <summary>
        /// CI 相关
        /// </summary>
        public void OnGUI_BuildpipelineCI()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("CI相关测试:");
                EditorGUILayout.HelpBox(@"服务器CI建议:
1.每次美术资源提交，自动构建AB，并且建议编写资源检测脚本,通过允许提交.
2.AssetBundle建议使用SVN版本管理.
3.发布母包会从SVN的资源到StreamingAsset.
4.发布增量资源，从SVN全部推送到资源服务器，客户端会对比两个版本Config下载本地没有的资源到Persistent下.客户端会进行可寻址加载.
", MessageType.Info);
                BDEditorApplication.BDFrameWorkFrameEditorSetting.WorkFollow.AssetBundleSVNUrl = EditorGUILayout.TextField("SVN地址",  BDEditorApplication.BDFrameWorkFrameEditorSetting.WorkFollow.AssetBundleSVNUrl, GUILayout.Width(350));
                BDEditorApplication.BDFrameWorkFrameEditorSetting.WorkFollow.AssetBundleSVNAccount = EditorGUILayout.TextField("SVN账号",  BDEditorApplication.BDFrameWorkFrameEditorSetting.WorkFollow.AssetBundleSVNAccount, GUILayout.Width(350));
                BDEditorApplication.BDFrameWorkFrameEditorSetting.WorkFollow.AssetBundleSVNPsw = EditorGUILayout.TextField("SVN密码",  BDEditorApplication.BDFrameWorkFrameEditorSetting.WorkFollow.AssetBundleSVNPsw, GUILayout.Width(350));
                
                //构建资源
                int Width = 100;
                GUILayout.Label("[构建资源]");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("IOS资源", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.BuildAssetBundle_iOS();
                    }

                    if (GUILayout.Button("Android资源", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.BuildAssetBundle_Android();
                    }
                }
                GUILayout.EndHorizontal();
                //构建dll
                GUILayout.Label("[代码检查]");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("DLL", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.BuildDLL();
                    }
                }
                GUILayout.EndHorizontal();
                //构建包体
                GUILayout.Label("[构建包体]");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("IOS-Release", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.PublishPackage_iOSRelease();
                    }

                    if (GUILayout.Button("IOS-Debug", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.PublishPackage_iOSDebug();
                    }

                    if (GUILayout.Button("Android-Release", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.PublishPackage_AndroidRelease();
                    }

                    if (GUILayout.Button("Android-Debug", GUILayout.Width(Width)))
                    {
                        PublishPipeLineCI.PublishPackage_AndroidDebug();
                    }
                }
            }
            GUILayout.EndVertical();
        }
    }
}