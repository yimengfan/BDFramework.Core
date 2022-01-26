using System;
using BDFramework.Editor.DevOps;
using BDFramework.Editor.EditorPipeline.DevOps;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 一键构建资源
    /// </summary>
    public class EditorWindow_CICD : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/DevOps/CI", false, (int) BDEditorGlobalMenuItemOrderEnum.DevOps)]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_CICD>(false, "CI");
            window.maxSize = window.minSize = new Vector2(800, 800);
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
                var devops_setting = BDEditorApplication.BDFrameWorkFrameEditorSetting.DevOpsSetting;
                devops_setting.AssetBundleSVNUrl = EditorGUILayout.TextField("SVN地址", devops_setting.AssetBundleSVNUrl, GUILayout.Width(350));
                devops_setting.AssetBundleSVNAccount = EditorGUILayout.TextField("SVN账号", devops_setting.AssetBundleSVNAccount, GUILayout.Width(350));
                devops_setting.AssetBundleSVNPsw = EditorGUILayout.TextField("SVN密码", devops_setting.AssetBundleSVNPsw, GUILayout.Width(350));

                GUILayout.Space(20);
                
                GUILayout.Label("支持CI列表:");

                //获取所有ciapi
                var ciMethods = DevOpsTools.GetCIApis();

                foreach (var cim in ciMethods)
                {
                    var attrs = cim.GetCustomAttributes(false);
                    var ciAttr = attrs[0] as CIAttribute;
                    GUILayout.BeginHorizontal();
                    {
                        //描述
                        GUILayout.Label(ciAttr.Des+":", GUILayout.Width(150));
                        
                        //函数
                        var ciName = cim.ReflectedType.FullName + "." + cim.Name;
                        GUILayout.Label(ciName,GUILayout.Width(580));
                        //按钮
                        if (GUILayout.Button("复制",GUILayout.Width(50)))
                        {
                            GUIUtility.systemCopyBuffer = ciName;
                            EditorUtility.DisplayDialog("提示", "复制成功!", "OK");
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }
    }
}