using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Tools;
using BDFramework.Core.Tools;
using BDFramework.Editor.DevOps;
using Sirenix.OdinInspector.Editor;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// 编辑器窗口 - 生成热更DLL
    /// </summary>
    public class EditorWindow_BuildHotfixDll : OdinEditorWindow
    {
        public void OnGUI()
        {
            if (BDEditorApplication.EditorSetting == null)
            {
                return;
            }

            //
            var buildDLLSetting = BDEditorApplication.EditorSetting?.BuildHotfixDLLSetting;
            GUILayout.BeginVertical();
            {
                GUILayout.Label("1.脚本打包", EditorGUIHelper.LabelH2);
                GUILayout.Space(5);


#if ENABLE_HYCLR

                GUILayout.Label("已启用HyCLR宏!");
#else
                  GUILayout.Label("未HyCLR宏,请手动点击下方按钮设置!");
#endif

                GUILayout.BeginHorizontal();
                {
                    GUI.color = Color.green;
                    if (GUILayout.Button("添加配置:BDFrameowork->HyCLR", GUILayout.Width(200), GUILayout.Height(20)))
                    {
                        BuildTools_HotfixScript.SetHyCLRConfig();
                    }

                    GUI.color = GUI.backgroundColor;
                }
                GUILayout.EndHorizontal();


                GUILayout.Space(5);
                //第二排
                GUILayout.BeginHorizontal();
                {
                    //
                    if (GUILayout.Button("1.编译热更dll", GUILayout.Width(155), GUILayout.Height(30)))
                    {
                        BuildTools_HotfixScript.BuildDLL(BApplication.streamingAssetsPath, BApplication.RuntimePlatform);
                    }

                    // if (GUILayout.Button("编译dll(Roslyn-Debug)", GUILayout.Width(150), GUILayout.Height(30)))
                    // {
                    //     BuildTools_HotfixScript.BuildDLL(BApplication.streamingAssetsPath, BApplication.RuntimePlatform, Unity3dRoslynBuildTools.BuildMode.Debug);
                    // }
                }
                GUILayout.EndHorizontal();



                if (BDEditorApplication.EditorSetting != null)
                {
                    buildDLLSetting.IsAutoBuildDll = EditorGUILayout.Toggle("是否自动编译热更DLL", buildDLLSetting.IsAutoBuildDll);
                }

                GUI.color = Color.green;
                GUILayout.Label(@"
注意事项:    
     1.编译服务使用Roslyn,请放心使用
     2.如编译出现报错，请仔细看报错信息,和报错的代码行列,
       一般均为语法错
     3.语法报错原因可能有:
       i.主工程访问hotfix中的类,
       ii.使用宏编译时代码结构发生变化
       ...
       等等，需要细心的你去发现");
                GUI.color = GUI.backgroundColor;
            }
            GUILayout.EndVertical();
        }


        private void OnDisable()
        {
            //保存
            BDEditorApplication.EditorSetting?.Save();
        }
    }
}