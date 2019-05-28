using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BDFramework;
using BDFramework.Editor.Tools;
using BDFramework.GameStart;
using BDFramework.Helper;
using ILRuntime.Runtime.CLRBinding;
using Mono.Cecil;
using Sirenix.OdinInspector;
using Tool;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using  BDFramework.DataListener;
public class EditorWindow_ScriptBuildDll : EditorWindow
{
    public void OnGUI()
    {
        GUILayout.BeginVertical();
        {
            GUILayout.Label("1.脚本打包", EditorGUIHelper.TitleStyle);
            GUILayout.Space(5);
            //第二排
            GUILayout.BeginHorizontal();
            {
                //
                if (GUILayout.Button("1.编译dll (.net版)", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    string source = Application.dataPath;
                    string outpath = Application.streamingAssetsPath + "/" +
                                     Utils.GetPlatformPath(Application.platform);
                    //
                    var hotfix = outpath + "/hotfix";
                    if (Directory.Exists(hotfix))
                    {
                        Directory.Delete(hotfix, true);
                    }

                    //
                    ScriptBuildTools.BuildDLL_DotNet(source, outpath);
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("[mono版]", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    //
                    //u3d的 各种dll
                    ScriptBuildTools.GenDllByMono(Application.dataPath, Application.streamingAssetsPath + "/" + Utils.GetPlatformPath(Application.platform));
                    Debug.Log("脚本打包完毕");
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("2.生成CLRBinding · one for all", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCLRBindingByAnalysis();
            }

            if (GUILayout.Button("3.生成跨域Adapter[没事别瞎点]", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCrossBindAdapter();
            }

            if (GUILayout.Button("4.生成Link.xml", GUILayout.Width(305), GUILayout.Height(30)))
            {
                StripCode.GenLinkXml();
            }

            GUI.color = Color.green;
            GUILayout.Label(
                @"注意事项:
     1.编译服务使用codedom,请放心使用
     2.如编译出现报错，请仔细看报错信息,和报错的代码行列,
       一般均为语法错
     3.语法报错原因可能有:主工程访问hotfix中的类, 使用宏
       编译时代码结构发生变化..等等，需要细心的你去发现
"
            );
            GUI.color = GUI.backgroundColor;
        }
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 生成类适配器
    /// </summary>
    static void GenCrossBindAdapter()
    {
        var types = new List<Type>();
        types.Add((typeof(UnityEngine.ScriptableObject)));
        types.Add((typeof(System.Exception)));
        types.Add(typeof(System.Collections.IEnumerable));
        types.Add(typeof(System.Runtime.CompilerServices.IAsyncStateMachine));
        types.Add(typeof(IGameStart));
        types.Add(typeof(ADataListener));
        types.Add(typeof(SerializedMonoBehaviour));
        GenAdapter.CreateAdapter(types, "Assets/Code/Game/ILRuntime/Adapter");
    }

    //生成clr绑定
    static void GenCLRBindingByAnalysis()
    {
        //用新的分析热更dll调用引用来生成绑定代码
        ILRuntimeHelper.LoadHotfix(Application.streamingAssetsPath,false);
        BindingCodeGenerator.GenerateBindingCode(ILRuntimeHelper.AppDomain,
            "Assets/Code/Game/ILRuntime/Binding/Analysis");
        AssetDatabase.Refresh();
        return;
        //暂时先不处理
        //预先绑定所有的
        var assemblies = new List<Assembly>()
        {
            typeof(UnityEngine.UI.Button).Assembly,
        };
        var types = new List<Type>();
        //
        foreach (var assm in assemblies)
        {
            var _ts = assm.GetTypes();
            foreach (var t in _ts)
            {
                if (t.Namespace != null)
                {
                    if (t.FullName.Contains("UnityEngine.Android")
                        || t.FullName.Contains("UnityEngine.iPhone")
                        || t.FullName.Contains("UnityEngine.WSA")
                        || t.FullName.Contains("UnityEngine.iOS")
                        || t.FullName.Contains("UnityEngine.Windows")
                        || t.FullName.Contains("JetBrains")
                        || t.FullName.Contains("Editor"))
                    {
                        continue;
                    }
                }


                types.Add(t);
            }
        }

        types = types.Distinct().ToList();
        //PreBinding 
        BindingCodeGenerator.GenerateBindingCode(types, "Assets/Code/Game/ILRuntime/Binding/PreBinding");
        AssetDatabase.Refresh();
    }
}