using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Unity3dEx;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 一键构建资源
    /// </summary>
    public class EditorWindow_CICD : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/DevOps/CI设置", false, (int) BDEditorGlobalMenuItemOrderEnum.DevOps)]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_CICD>(false, "CI");
            window.maxSize = window.minSize = new Vector2(850, 800);
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


        Vector2 pos = Vector2.zero;

        /// <summary>
        /// CI 相关
        /// </summary>
        public void OnGUI_BuildpipelineCI()
        {
            GUILayout.BeginVertical();
            {
                var devops_setting = BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildSetting;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label("资源仓库地址:");

                    devops_setting.AssetServiceVCSData.Url = EditorGUILayout.TextField("地址", devops_setting.AssetServiceVCSData.Url, GUILayout.Width(350));
                    devops_setting.AssetServiceVCSData.UserName = EditorGUILayout.TextField("账号", devops_setting.AssetServiceVCSData.UserName, GUILayout.Width(350));
                    devops_setting.AssetServiceVCSData.Psw = EditorGUILayout.TextField("密码", devops_setting.AssetServiceVCSData.Psw, GUILayout.Width(350));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical();
                    GUILayout.Label("母包仓库地址:");
                    devops_setting.PackageServiceVCSData.Url = EditorGUILayout.TextField("地址", devops_setting.PackageServiceVCSData.Url, GUILayout.Width(350));
                    devops_setting.PackageServiceVCSData.UserName = EditorGUILayout.TextField("账号", devops_setting.PackageServiceVCSData.UserName, GUILayout.Width(350));
                    devops_setting.PackageServiceVCSData.Psw = EditorGUILayout.TextField("密码", devops_setting.PackageServiceVCSData.Psw, GUILayout.Width(350));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(20);

                GUILayout.Label("支持CI列表:");
                EditorGUILayoutEx.Layout_DrawLineH(Color.white, 2f);
                //获取所有ciapi
                var ciMethods = DevOpsTools.GetCIApis();
                pos = EditorGUILayout.BeginScrollView(pos, GUILayout.Width(850), GUILayout.Height(500));
                {
                    foreach (var cim in ciMethods)
                    {
                        var attrs = cim.GetCustomAttributes(false);
                        var ciAttr = attrs[0] as CIAttribute;
                        GUILayout.BeginHorizontal();
                        {
                            //描述
                            GUILayout.Label(ciAttr.Des + ":", GUILayout.Width(150));
                            //函数
                            var ciName = cim.ReflectedType.FullName + "." + cim.Name;
                            GUILayout.Label(ciName, GUILayout.Width(580));
                            //按钮
                            if (GUILayout.Button("复制", GUILayout.Width(50)))
                            {
                                GUIUtility.systemCopyBuffer = ciName;
                                EditorUtility.DisplayDialog("提示", "复制成功!\n" + cim.Name, "OK");
                            }

                            if (GUILayout.Button("执行", GUILayout.Width(50)))
                            {
                                var ret = EditorUtility.DisplayDialog("提示", "是否执行:" + cim.Name, "OK", "Cancel");
                                if (ret)
                                {
                                    //执行
                                    cim.Invoke(null, new object[] { });
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayoutEx.Layout_DrawLineH(Color.white);


                GUILayout.Space(10);
                EditorGUILayoutEx.Layout_DrawLineH(Color.white);

                GUILayout.Label(@"服务器CI流程:
一般Git管理代码，SVN或P4管理美术资产。
Git master分支作为稳定发布版本分支，工作都在子分支，测试通过后会合并到主分支。
SVN资产也会用hook实现同步到Git assets分支，供程序使用. 程序也会将测试通过的资产随着code提交到主分支.
CI一般监听Git Master分支，定时一键构建所有资产:AB包、脚本、Sql

1.资源流程: master提交=>生成AB、热更脚本、sql=>AB性能测试=>WebHook通知到内部=>提交到资产SVN
2.母包流程: 更新资产SVN、更新Git master=>构建母包=>自动包体测试=>通知测试结果
3.资源发布: 更新资产SVN=>发布到资源服务器
");
            }
            GUILayout.EndVertical();
        }
    }
}