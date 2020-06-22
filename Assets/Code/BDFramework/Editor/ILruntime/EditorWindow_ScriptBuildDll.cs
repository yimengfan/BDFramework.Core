using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using BDFramework;
using BDFramework.Editor.Tools;
using BDFramework.GameStart;
using ILRuntime.Runtime.CLRBinding;
using Tool;
using Debug = UnityEngine.Debug;
using BDFramework.DataListener;
using BDFramework.Editor;
using Code.BDFramework.Core.Tools;
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
                GenCLRBinding();
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
    static public void RoslynBuild(ScriptBuildTools.BuildMode mode,string outpath =null)
    {
        var targetPath = "Assets/Code/Game/ILRuntime/Binding/Analysis";
        //分析之前先删除,然后生成临时文件防止报错
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }
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

        //没指定，走默认模式
        if (string.IsNullOrEmpty(outpath))
        {
            //1.build dll
            var outpath_win = Application.streamingAssetsPath + "/" + BApplication.GetPlatformPath(Application.platform);
            ScriptBuildTools.BuildDll(outpath_win, mode);
            //2.同步到其他两个目录
            var outpath_android = Application.streamingAssetsPath + "/" + BApplication.GetPlatformPath(RuntimePlatform.Android)      + DLLPATH;
            var outpath_ios     = Application.streamingAssetsPath + "/" + BApplication.GetPlatformPath(RuntimePlatform.IPhonePlayer) + DLLPATH;

            var source = outpath_win + DLLPATH;
            var bytes  = File.ReadAllBytes(source);
            if (source != outpath_android)
                FileHelper.WriteAllBytes(outpath_android, bytes);
            if (source != outpath_ios)
                FileHelper.WriteAllBytes(outpath_ios, bytes);
        }
        else
        {
            //指定了直接 build
            ScriptBuildTools.BuildDll(outpath, mode);
        }
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



    
    static Type[] manualBindingTypes = new Type[]
    {
        typeof(MethodBase), typeof(MemberInfo), typeof(FieldInfo), typeof(MethodInfo),
        typeof(PropertyInfo), typeof(Component), typeof(Type), typeof(Debug)
    };

    /// <summary>
    /// 分析dll生成
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="dllpath"></param>
    static private void GenCLRBindingByAnalysis(RuntimePlatform platform = RuntimePlatform.Lumin, string dllpath = "")
    {
        if (platform == RuntimePlatform.Lumin)
        {
            platform = Application.platform;
        }

        //默认读StreammingAssets下面path
        if (dllpath == "")
        {
            dllpath = Application.streamingAssetsPath + "/" + BApplication.GetPlatformPath(platform) + DLLPATH;
        }

        //不参与自动绑定的
        List<Type> excludeTypes = new List<Type>(); //
        excludeTypes.AddRange(manualBindingTypes);
        excludeTypes.AddRange(preBindingTypes);

        //用新的分析热更dll调用引用来生成绑定代码
        var targetPath = "Assets/Code/Game/ILRuntime/Binding/Analysis";
        ILRuntimeHelper.LoadHotfix(dllpath, false);
        BindingCodeGenerator.GenerateAnalysisBindingCode(ILRuntimeHelper.AppDomain, targetPath,
                                                         blackTypes: excludeTypes);

        ILRuntimeHelper.Close();
        AssetDatabase.Refresh();
        //暂时先不处理
    }
    
    
    static List<Type> preBindingTypes = new List<Type>();
      /// <summary>
      /// 黑名单
      /// </summary>
      static List<Type> blackTypeList =new List<Type>()
      {
          typeof(UnityEngine.UI.GraphicRebuildTracker),
          typeof(UnityEngine.UI.Graphic),
          typeof(UnityEngine.UI.DefaultControls)
      };
      /// <summary>
      /// 方法黑名单
      /// </summary>
      static HashSet<MethodBase> blackMethodList = new HashSet<MethodBase>()
      { //Text
        typeof(Text).GetMethod(nameof(Text.OnRebuildRequested)),
        //TODO Others
      };
    /// <summary>
    /// 生成预绑定
    /// </summary>
    static private void GenPreCLRBinding()
    {
        preBindingTypes = new List<Type>();
        var types = typeof(Button).Assembly.GetTypes().ToList(); //所有UI相关接口预绑定
        //移除黑名单
        foreach (var blackType in blackTypeList)
        {
            types.Remove(blackType);
        }
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
                                                 outputPath: "Assets/Code/Game/ILRuntime/Binding/PreBinding",
                                                 excludeMethods: blackMethodList);
    }

    /// <summary>
    /// 生成手动绑定部分
    /// </summary>
    static private void GenCLRBinding()
    {
        var types = new List<Type>();
        //反射类优先生成
        types.Add(typeof(UnityEngine.Debug));
        //PreBinding 
        BindingCodeGenerator.GenerateBindingCode(types, "Assets/Code/Game/ILRuntime/Binding/PreBinding");
        AssetDatabase.Refresh();
    }
}