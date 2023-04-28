using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Tools;
using BDFramework.Core.Tools;
using BDFramework.Editor.DevOps;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// 编辑器窗口 - 生成热更DLL
    /// </summary>
    public class EditorWindow_BuildHotfixDll : EditorWindow
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

#if !ENABLE_ILRUNTIME && !ENABLE_HCLR
                HotfixScriptEditorTools.SwitchToHCLR();
#endif

#if ENABLE_HCLR
                GUILayout.Label("当前模式:HCLR");
#elif ENABLE_ILRUNTIME
                GUILayout.Label("当前模式:ILRuntime
#endif
                
                GUILayout.BeginHorizontal();
                {
#if ENABLE_ILRUNTIME
 GUI.color = Color.green;
#endif
                    if (GUILayout.Button("切换ILRuntime", GUILayout.Width(155), GUILayout.Height(20)))
                    {
                        HotfixScriptEditorTools.SwitchToILRuntime();
                    }
#if ENABLE_HCLR
                    GUI.color = Color.green;
#endif
                    if (GUILayout.Button("切换HCLR", GUILayout.Width(150), GUILayout.Height(20)))
                    {
                        HotfixScriptEditorTools.SwitchToHCLR();
                    }

                    GUI.color = GUI.backgroundColor;
                }
                GUILayout.EndHorizontal();


                GUILayout.Space(5);
                //第二排
                GUILayout.BeginHorizontal();
                {
                    //
                    if (GUILayout.Button("1.编译dll(Roslyn-Release)", GUILayout.Width(155), GUILayout.Height(30)))
                    {
                        HotfixScriptEditorTools.RoslynBuild(Application.streamingAssetsPath, BApplication.RuntimePlatform, Unity3dRoslynBuildTools.BuildMode.Release);
                    }

                    if (GUILayout.Button("编译dll(Roslyn-Debug)", GUILayout.Width(150), GUILayout.Height(30)))
                    {
                        HotfixScriptEditorTools.RoslynBuild(Application.streamingAssetsPath, BApplication.RuntimePlatform, Unity3dRoslynBuildTools.BuildMode.Debug);
                    }
                }
                GUILayout.EndHorizontal();

#if ENABLE_ILRUNTIME
                if (GUILayout.Button("2.生成跨域Adapter[没事别瞎点]", GUILayout.Width(305), GUILayout.Height(30)))
                {
                    HotfixScriptEditorTools.GenCrossBindAdapter();
                }

                if (GUILayout.Button("3.生成Link.xml[大部分不需要]", GUILayout.Width(305), GUILayout.Height(30)))
                {
                    StripCode.GenLinkXml();
                }

                if (GUILayout.Button("4.预检查工程代码", GUILayout.Width(305), GUILayout.Height(30)))
                {
                    PublishPipeLineCI.CheckEditorCode();
                }
#endif

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