using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using BDFramework;
using ILRuntime.Runtime.CLRBinding;
using Debug = UnityEngine.Debug;

public class EditorWindow_ScriptBuildDll: EditorWindow
{


    private void OnGUI()
    {
        GUILayout.BeginVertical();
        {
            GUILayout.Space(20);
            //第二排
            GUILayout.BeginHorizontal();
            {
                //
                if (GUILayout.Button("1.编译dll (.net版)", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    string str1 = Application.dataPath;
                    string str2 = Application.streamingAssetsPath;
                    Debug.Log(str1);
                    Debug.Log(str2);
                    string exePath = Application.dataPath + "/" + "Code/BDFramework/Tools/ILRBuild/build.exe";
                    if (File.Exists(exePath))
                    {
                        Debug.Log(".net编译工具存在!");
                    }

                    //命令行内 路径不允许有空格，所以用引号引起来
                    var u3dUI = @"""D:\Program Files\Unity2018.2.0f2\Editor\Data\UnityExtensions\Unity\GUISystem""";
                    var u3dEngine = @"""D:\Program Files\Unity2018.2.0f2\Editor\Data\Managed\UnityEngine""";

                    if (Directory.Exists(u3dUI.Replace(@"""","")) == false || Directory.Exists(u3dEngine.Replace(@"""","")) == false)
                    {
                        EditorUtility.DisplayDialog("提示", "请修改u3dui 和u3dengine 的dll目录", "OK");
                        return;
                    }
                    //
                    Process.Start(exePath, string.Format("{0} {1} {2} {3}", str1, str2, u3dUI, u3dEngine));
                    
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("[mono版]", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    //u3d的 各种dll
                    var u3dUI = @"D:\Program Files\Unity2018.2.0f2\Editor\Data\UnityExtensions\Unity\GUISystem";
                    var u3dEngine = @"D:\Program Files\Unity2018.2.0f2\Editor\Data\Managed\UnityEngine";
                    if (Directory.Exists(u3dUI) == false || Directory.Exists(u3dEngine) == false)
                    {
                        EditorUtility.DisplayDialog("提示", "请修改u3dui 和u3dengine 的dll目录", "OK");
                    }

                    ScriptBiuld_Service.BuildDLL_Mono(Application.dataPath, Application.streamingAssetsPath, u3dUI,
                        u3dEngine);
                }
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("2.生成CLRBinding", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCLRBindingByAnalysis();
            }
            if (GUILayout.Button("3.生成跨域Adapter[没事别瞎点]", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCrossBindAdapter();
            }

        }
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 生成类适配器
    /// </summary>
    static void GenCrossBindAdapter()
    {
        var types =  new List<Type>();
        types.Add((typeof(UnityEngine.ScriptableObject)));
        types.Add((typeof(System.Exception)));
        types.Add(typeof(System.Collections.IEnumerable));
        GenAdapter.CreateAdapter(types,"Assets/Code/Game/ILRuntime/Adapter");
        
    }
    
    
    
    //生成clr绑定
    static void GenCLRBindingByAnalysis()
    {
        //用新的分析热更dll调用引用来生成绑定代码
        ILRuntimeHelper.LoadHotfix(false);
        BindingCodeGenerator.GenerateBindingCode(ILRuntimeHelper.AppDomain, "Assets/Code/Game/ILRuntime/Binding");
        AssetDatabase.Refresh();
    }

}
