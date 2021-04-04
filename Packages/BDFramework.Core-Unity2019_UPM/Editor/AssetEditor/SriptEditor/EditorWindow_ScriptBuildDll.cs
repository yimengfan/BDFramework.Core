using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using BDFramework;
using BDFramework.AssetHelper;
using BDFramework.Editor.Tools;
using BDFramework.GameStart;
using Tool;
using Debug = UnityEngine.Debug;
using BDFramework.DataListener;
using BDFramework.Editor;
using BDFramework.Editor.EditorLife;
using BDFramework.Core.Tools;
using ILRuntime.Runtime.CLRBinding;
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
                    RoslynBuild(Application.streamingAssetsPath, Application.platform,
                        ScriptBuildTools.BuildMode.Release);
                }

                if (GUILayout.Button("编译dll(Roslyn-Debug)", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    RoslynBuild(Application.streamingAssetsPath, Application.platform,
                        ScriptBuildTools.BuildMode.Debug);
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("2.生成跨域Adapter[没事别瞎点]", GUILayout.Width(305), GUILayout.Height(30)))
            {
                GenCrossBindAdapter();
            }

            if (GUILayout.Button("3.生成Link.xml[大部分不需要]", GUILayout.Width(305), GUILayout.Height(30)))
            {
                StripCode.GenLinkXml();
            }

            BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.IsAutoBuildDll =
                EditorGUILayout.Toggle("是否自动编译热更DLL",BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.IsAutoBuildDll );

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
    /// <param name="outpath"></param>
    /// <param name="platform"></param>
    /// <param name="mode"></param>
    static public void RoslynBuild(string outpath, RuntimePlatform platform, ScriptBuildTools.BuildMode mode,bool isShowTips =true)
    {
        //触发bd环境周期
        BDFrameEditorBehaviorHelper.OnBeginBuildDLL();

        var targetPath = "Assets/Code/BDFramework.Game/ILRuntime/Binding/Analysis";
        //1.分析之前先删除,然后生成临时文件防止报错
        // if (Directory.Exists(targetPath))
        // {
        //     Directory.Delete(targetPath, true);
        // }
        
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

        //2.生成DLL
        ScriptBuildTools.BuildDll(outpath, platform, mode,isShowTips);

        //3.预绑定
        //GenPreCLRBinding();
        //4.生成自动分析绑定
        GenCLRBindingByAnalysis(platform, outpath);
        AssetDatabase.Refresh();
        //触发bd环境周期
        BDFrameEditorBehaviorHelper.OnEndBuildDLL(outpath);
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
        GenAdapter.CreateAdapter(types, "Assets/Code/BDFramework.Game/ILRuntime/Adapter");
    }


    static Type[] manualBindingTypes = new Type[]
    {
        //typeof(MethodBase), typeof(MemberInfo), typeof(FieldInfo), typeof(MethodInfo), typeof(PropertyInfo),
        //typeof(Component), typeof(Type), typeof(Debug)
    };

    /// <summary>
    /// 分析dll生成
    /// </summary>
    /// <param name="platform"></param>
    /// <param name="dllpath"></param>
    static private void GenCLRBindingByAnalysis(RuntimePlatform platform = RuntimePlatform.Lumin, string dllpath = "")
    {
        //默认参数
        if (platform == RuntimePlatform.Lumin)
        {
            platform = Application.platform;
        }

        if (dllpath == "")
        {
            dllpath = Application.streamingAssetsPath;
        }

        //路径
        dllpath = dllpath + "/" + BDApplication.GetPlatformPath(platform) + DLLPATH;
        //不参与自动绑定的
        List<Type> excludeTypes = new List<Type>(); //
        excludeTypes.AddRange(manualBindingTypes);
        excludeTypes.AddRange(preBindingTypes);
        //用新的分析热更dll调用引用来生成绑定代码
        var outputPath = "Assets/Code/BDFramework.Game/ILRuntime/Binding/Analysis";
        //游戏工程的Bind
        Action<bool> mainProjectIlrBindAction = null;
        var type = BDFrameEditorLife.Types.FirstOrDefault((t) => t.FullName == "Game.ILRuntime.GameLogicILRBinding");
        if (type != null)
        {
            var method = type.GetMethod("Bind", BindingFlags.Public | BindingFlags.Static);
            Delegate bindDelegate = Delegate.CreateDelegate(typeof(Action<bool>), null, method);
            mainProjectIlrBindAction = bindDelegate as Action<bool>;
        }
        //注册
        ILRuntimeHelper.LoadHotfix(dllpath, mainProjectIlrBindAction, false);
        BindingCodeGenerator.GenerateBindingCode(ILRuntimeHelper.AppDomain, outputPath);
        ILRuntimeHelper.Close();


        /******************移除已经被绑定的部分****************/
        var analysisClrBinding = IPath.Combine(outputPath, "CLRBindings.cs");
        var manualPath = "Assets/Code/BDFramework.Game/ILRuntime/Binding/Manual";
        var prebindingPath = "Assets/Code/BDFramework.Game/ILRuntime/Binding/PreBinding";
        //手动绑定的所有文件
        var bindingFs = Directory.GetFiles(manualPath, "*.*").ToList();
        if (Directory.Exists(prebindingPath))
        {
            bindingFs.AddRange(Directory.GetFiles(prebindingPath, "*.*"));
        }

        for (int i = 0; i < bindingFs.Count; i++)
        {
            //删除被手动绑定的文件
            var f = IPath.Combine(outputPath, Path.GetFileName(bindingFs[i]));
            if (f != analysisClrBinding && File.Exists(f))
            {
                File.Delete(f);
            }

            bindingFs[i] = Path.GetFileNameWithoutExtension(bindingFs[i]);
        }

        var analysisContent = File.ReadAllLines(analysisClrBinding).ToList();
        //修改CLRbindding内容
        for (int i = analysisContent.Count - 1; i >= 0; i--)
        {
            var line = analysisContent[i];
            //移除line
            foreach (var mf in bindingFs)
            {
                if (line.Contains(mf + ".Register(app);"))
                {
                    analysisContent.RemoveAt(i);
                    Debug.Log("移除[已经绑定]:" + line);
                    break;
                }
            }
        }

        //写入
        File.WriteAllLines(analysisClrBinding, analysisContent);

        //Manual

        AssetDatabase.Refresh();
    }


    static List<Type> preBindingTypes = new List<Type>();

    /// <summary>
    /// 黑名单
    /// </summary>
    static List<Type> blackTypeList = new List<Type>()
    {
        typeof(UnityEngine.UI.GraphicRebuildTracker),
        typeof(UnityEngine.UI.Graphic),
        typeof(UnityEngine.UI.DefaultControls)
    };

    /// <summary>
    /// 方法黑名单
    /// </summary>
    static HashSet<MethodBase> blackMethodList = new HashSet<MethodBase>()
    {
        //Text
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

        var output = "Assets/Code/BDFramework.Game/ILRuntime/Binding/PreBinding";
        var clrbinding = IPath.Combine(output, "CLRBindings.cs");
        var prebinding = IPath.Combine(output, "PreCLRBinding.cs");
        //
        BindingCodeGenerator.GenerateBindingCode(preBindingTypes, output, excludeMethods: blackMethodList);
        var oldContent = File.ReadAllText(clrbinding);
        var newContent = oldContent.Replace("class CLRBindings", "class PreCLRBinding");
        //写入新的,删除老的
        File.WriteAllText(prebinding, newContent);
        File.Delete(clrbinding);
    }
}