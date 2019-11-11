using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Reflection;
using BDFramework;
using BDFramework.Editor.Tools;
using BDFramework.GameStart;
using BDFramework.Helper;
using ILRuntime.Runtime.CLRBinding;
using Tool;
using Debug = UnityEngine.Debug;
using BDFramework.DataListener;
using BDFramework.Editor;

public class EditorWindow_ScriptBuildDll : EditorWindow
{
    [MenuItem("BDFrameWork工具箱/1.DLL打包", false, (int) BDEditorMenuEnum.BuildPackage_DLL)]
    public static void Open()
    {
        var window =
            (EditorWindow_ScriptBuildDll) EditorWindow.GetWindow(typeof(EditorWindow_ScriptBuildDll), false,
                "DLL打包工具");
        window.Show();
    }

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
                if (GUILayout.Button("1.编译dll(Roslyn-Release)", GUILayout.Width(155), GUILayout.Height(30)))
                {
                    RoslynBuild(ScriptBuildTools.BuildMode.Release);
                }

                if (GUILayout.Button("编译dll(Roslyn-Debug)", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    RoslynBuild(ScriptBuildTools.BuildMode.Debug);
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("2.生成CLRBinding · one for all[已集成]", GUILayout.Width(305), GUILayout.Height(30)))
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
                @"
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


    /// <summary>
    /// 编译模式
    /// </summary>
    /// <param name="mode"></param>
    static void RoslynBuild(ScriptBuildTools.BuildMode mode)
    {
        //1.build dll
        var outpath_win = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(Application.platform);
        ScriptBuildTools.BuildDll(Application.dataPath, outpath_win, mode);
        //2.同步到其他两个目录
        var outpath_android = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(RuntimePlatform.Android) + "/hotfix/hotfix.dll";
        var outpath_ios = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(RuntimePlatform.IPhonePlayer) + "/hotfix/hotfix.dll";
                    
        var source = outpath_win + "/hotfix/hotfix.dll";
        var bytes = File.ReadAllBytes(source);
        if (source != outpath_android)
            FileHelper.WriteAllBytes(outpath_android,bytes);
        if (source != outpath_ios)
            FileHelper.WriteAllBytes(outpath_ios,bytes);

        //3.生成CLRBinding
        GenCLRBindingByAnalysis();
        AssetDatabase.Refresh();
        Debug.Log("脚本打包完毕");
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
        //types.Add(typeof(SerializedMonoBehaviour));
        GenAdapter.CreateAdapter(types, "Assets/Code/Game/ILRuntime/Adapter");
    }

    //生成clr绑定
    static public void GenCLRBindingByAnalysis(RuntimePlatform platform = RuntimePlatform.Lumin)
    {
        if (platform == RuntimePlatform.Lumin)
        {
            platform = Application.platform;
        }

        //用新的分析热更dll调用引用来生成绑定代码
        var dllpath = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(platform) + "/hotfix/hotfix.dll";
        ILRuntimeHelper.LoadHotfix(dllpath, false);
        BindingCodeGenerator.GenerateBindingCode(ILRuntimeHelper.AppDomain,
            "Assets/Code/Game/ILRuntime/Binding/Analysis");
        AssetDatabase.Refresh();
        return;
        //暂时先不处理
        var assemblies = new List<Assembly>()
        {
            typeof(UnityEngine.UI.Button).Assembly,
        };
        var types = new List<Type>();
        types.Add(typeof(Vector4));
        //
//        foreach (var assm in assemblies)
//        {
//            var _ts = assm.GetTypes();
//            foreach (var t in _ts)
//            {
//                if (t.Namespace != null)
//                {
//                    if (t.FullName.Contains("UnityEngine.Android")
//                        || t.FullName.Contains("UnityEngine.iPhone")
//                        || t.FullName.Contains("UnityEngine.WSA")
//                        || t.FullName.Contains("UnityEngine.iOS")
//                        || t.FullName.Contains("UnityEngine.Windows")
//                        || t.FullName.Contains("JetBrains")
//                        || t.FullName.Contains("Editor"))
//                    {
//                        continue;
//                    }
//                }
//
//
//                types.Add(t);
//            }
//        }
//
//        types = types.Distinct().ToList();
        //PreBinding 
        BindingCodeGenerator.GenerateBindingCode(types, "Assets/Code/Game/ILRuntime/Binding/PreBinding");
        AssetDatabase.Refresh();
    }
}