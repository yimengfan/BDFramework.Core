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
using UnityEngine.UI;

public class EditorWindow_ScriptBuildDll : EditorWindow
{
    [MenuItem("BDFrameWork工具箱/1.DLL打包", false, (int) BDEditorMenuEnum.BuildPackage_DLL)]
    public static void Open()
    {
        var window =
            (EditorWindow_ScriptBuildDll) EditorWindow.GetWindow(typeof(EditorWindow_ScriptBuildDll), false, "DLL打包工具");
        window.Show();
    }

    private static string DLLPATH = "/Hotfix/hotfix.dll";

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

            if (GUILayout.Button("2.分析DLL生成绑定", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCLRBindingByAnalysis();
            }

            if (GUILayout.Button("2.1 手动绑定生成", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCLRBindingBySelf();
            }

            if (GUILayout.Button("2.2 生成预绑定（如所有UI组件等）", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenPreCLRBinding();
            }


            if (GUILayout.Button("3.生成跨域Adapter[没事别瞎点]", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCrossBindAdapter();
            }

            if (GUILayout.Button("4.生成Link.xml[大部分不需要]", GUILayout.Width(305), GUILayout.Height(30)))
            {
                StripCode.GenLinkXml();
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


    /// <summary>
    /// 编译模式
    /// </summary>
    /// <param name="mode"></param>
    static public void RoslynBuild(ScriptBuildTools.BuildMode mode)
    {
        var targetPath = "Assets/Code/Game/ILRuntime/Binding/Analysis";
        //分析之前先删除,然后生成临时文件防止报错
        Directory.Delete(targetPath, true);
        var fileContent = @"
namespace ILRuntime.Runtime.Generated
{
    class CLRBindings
    {
        public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
        }
    } 
}   ";
        FileHelper.WriteAllText(targetPath + "/CLRBindings.cs", fileContent);
        AssetDatabase.Refresh(); //这里必须要刷新
        //1.build dll
        var outpath_win = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(Application.platform);
        ScriptBuildTools.BuildDll(Application.dataPath, outpath_win, mode);
        //2.同步到其他两个目录
        var outpath_android = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(RuntimePlatform.Android) +
                              DLLPATH;
        var outpath_ios = Application.streamingAssetsPath                       + "/" +
                          BDUtils.GetPlatformPath(RuntimePlatform.IPhonePlayer) + DLLPATH;

        var source = outpath_win + DLLPATH;
        var bytes  = File.ReadAllBytes(source);
        if (source != outpath_android)
            FileHelper.WriteAllBytes(outpath_android, bytes);
        if (source != outpath_ios)
            FileHelper.WriteAllBytes(outpath_ios, bytes);

        //3.预绑定
        GenPreCLRBinding();
        //4.生成AnalysisCLRBinding
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
        types.Add(typeof(Attribute));
        //types.Add(typeof(SerializedMonoBehaviour));
        GenAdapter.CreateAdapter(types, "Assets/Code/Game/ILRuntime/Adapter");
    }


    static List<Type> preBindingTypes = new List<Type>();

    /// <summary>
    /// 生成预绑定
    /// </summary>
    static public void GenPreCLRBinding()
    {
        preBindingTypes = new List<Type>();
        var types = typeof(Button).Assembly.GetTypes(); //所有UI相关接口预绑定

        foreach (var t in types)
        {
            if (t.IsClass && t.IsPublic && !t.IsEnum)
            {
                //除开被弃用的
                var attrs = t.GetCustomAttributes(typeof(System.ObsoleteAttribute), false);
                if (attrs.Length == 0)
                {
                    preBindingTypes.Add(t);
                }
            }
        }

        BindingCodeGenerator.GenerateBindingCode(preBindingTypes, clrbingdingClassName: "PreCLRBinding",
                                                 outputPath: "Assets/Code/Game/ILRuntime/Binding/PreBinding");
    }


    static Type[] manualBindingTypes = new Type[]
    {
        typeof(MethodBase), typeof(MemberInfo), typeof(FieldInfo), typeof(MethodInfo),
        typeof(PropertyInfo), typeof(Component), typeof(Type), typeof(Debug)
    };

    //生成clr绑定
    static public void GenCLRBindingByAnalysis(RuntimePlatform platform = RuntimePlatform.Lumin, string dllpath = "")
    {
        if (platform == RuntimePlatform.Lumin)
        {
            platform = Application.platform;
        }

        //默认读StreammingAssets下面path
        if (dllpath == "")
        {
            dllpath = Application.streamingAssetsPath + "/" + BDUtils.GetPlatformPath(platform) + DLLPATH;
        }

        //不参与自动绑定的
        List<Type> excludeTypes = new List<Type>(); //
        excludeTypes.AddRange(manualBindingTypes);
        excludeTypes.AddRange(preBindingTypes);

        //用新的分析热更dll调用引用来生成绑定代码
        var targetPath = "Assets/Code/Game/ILRuntime/Binding/Analysis";
        ILRuntimeHelper.LoadHotfix(dllpath, false);
        BindingCodeGenerator.GenerateAnalysisBindingCode(ILRuntimeHelper.AppDomain, targetPath,
                                                         excludeType: excludeTypes);

        ILRuntimeHelper.Close();
        AssetDatabase.Refresh();

        //暂时先不处理
    }

    static public void GenCLRBindingBySelf()
    {
        var types = new List<Type>();
        //反射类优先生成
        types.Add(typeof(UnityEngine.Debug));
        //PreBinding 
        BindingCodeGenerator.GenerateBindingCode(types, "Assets/Code/Game/ILRuntime/Binding/PreBinding");
        AssetDatabase.Refresh();
    }
}