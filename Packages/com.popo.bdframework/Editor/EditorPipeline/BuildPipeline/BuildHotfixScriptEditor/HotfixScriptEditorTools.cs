using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.Editor;
using BDFramework.Editor.Environment;
using BDFramework.Editor.Unity3dEx;
using BDFramework.GameStart;
using ILRuntime.Runtime.CLRBinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// 热更代码工具
    /// </summary>
    static public class HotfixScriptEditorTools
    {
        static string ENABLE_ILRUNTIME = "ENABLE_ILRUNTIME";
        static private string ENABLE_HCLR = "ENABLE_HCLR";

        /// <summary>
        /// 使用ILRuntime
        /// </summary>
        static public void SwitchToILRuntime()
        {
            Unity3dEditorEx.RemoveSymbols(ENABLE_HCLR);
            Unity3dEditorEx.AddSymbols(ENABLE_ILRUNTIME);
        }


        /// <summary>
        /// 使用ILRuntime
        /// </summary>
        static public void SwitchToHCLR()
        {
            Unity3dEditorEx.RemoveSymbols(ENABLE_ILRUNTIME);
            Unity3dEditorEx.AddSymbols(ENABLE_HCLR);
        }


        /// <summary>
        /// 编译模式
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="platform"></param>
        /// <param name="mode"></param>
        static public void RoslynBuild(string outpath, RuntimePlatform platform, ScriptBuildTools.BuildMode mode, bool isShowTips = true)
        {
            //触发bd环境周期
            BDFrameworkPipelineHelper.OnBeginBuildHotfixDLL();

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
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector2> s_UnityEngine_Vector2_Binding_Binder = null;
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector3> s_UnityEngine_Vector3_Binding_Binder = null;
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector4> s_UnityEngine_Vector4_Binding_Binder = null;
                internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Quaternion> s_UnityEngine_Quaternion_Binding_Binder = null;
                public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
                {
                }
            } 
        }   ";
            FileHelper.WriteAllText(targetPath + "/CLRBindings.cs", fileContent);
            AssetDatabase.Refresh(); //这里必须要刷新

            //2.生成DLL
            ScriptBuildTools.BuildDll(outpath, platform, mode, isShowTips);

            //3.预绑定
            //GenPreCLRBinding();
            //4.生成自动分析绑定
            // GenCLRBindingByAnalysis(platform, outpath);
            //5.拷贝
            CopyDLLToOther(outpath, platform);
            AssetDatabase.Refresh();
            //触发bd环境周期
            BDFrameworkPipelineHelper.OnEndBuildDLL(outpath);
        }

        /// <summary>
        /// 拷贝当前到其他目录
        /// </summary>
        /// <param name="sourceh"></param>
        public static void CopyDLLToOther(string root, RuntimePlatform sourcePlatform)
        {
            var source = ScriptLoder.GetLocalDLLPath(root, sourcePlatform);
            var bytes = File.ReadAllBytes(source);
            var sourcePdb = source + ".pdb";
            byte[] pdbBytes = null;
            if (File.Exists(sourcePdb))
            {
                pdbBytes = File.ReadAllBytes(sourcePdb);
            }

            //拷贝当前到其他目录
            foreach (var sp in BApplication.SupportPlatform)
            {
                var outpath = ScriptLoder.GetLocalDLLPath(root, sp);
                if (source == outpath)
                {
                    continue;
                }

                FileHelper.WriteAllBytes(outpath, bytes);
                //pdb
                if (pdbBytes != null)
                {
                    FileHelper.WriteAllBytes(outpath + ".pdb", pdbBytes);
                }
            }
        }


        /// <summary>
        /// 生成类适配器
        /// </summary>
        static public void GenCrossBindAdapter()
        {
            var types = new List<Type>();
            types.Add((typeof(UnityEngine.ScriptableObject)));
            types.Add((typeof(System.Exception)));
            types.Add(typeof(System.Collections.IEnumerable));
            types.Add(typeof(System.Runtime.CompilerServices.IAsyncStateMachine));
            types.Add(typeof(IGameStart));
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
        static public void GenCLRBindingByAnalysis(RuntimePlatform platform, string dllpath = "")
        {
            if (dllpath == "")
            {
                dllpath = Application.streamingAssetsPath;
            }

            //路径
            dllpath = IPath.Combine(dllpath, BApplication.GetPlatformPath(platform), ScriptLoder.DLL_PATH);
            //不参与自动绑定的
            List<Type> excludeTypes = new List<Type>(); //
            excludeTypes.AddRange(manualBindingTypes);
            excludeTypes.AddRange(preBindingTypes);
            //用新的分析热更dll调用引用来生成绑定代码
            var outputPath = "Assets/Code/BDFramework.Game/ILRuntime/Binding/Analysis";
            //游戏工程的Bind
            Action<bool> mainProjectIlrBindAction = null;
            var type = BDFrameworkEditorEnvironment.Types.FirstOrDefault((t) => t.FullName == "Game.ILRuntime.GameLogicCLRBinding");
            if (type != null)
            {
                var method = type.GetMethod("Bind", BindingFlags.Public | BindingFlags.Static);
                Delegate bindDelegate = Delegate.CreateDelegate(typeof(Action<bool>), null, method);
                mainProjectIlrBindAction = bindDelegate as Action<bool>;
            }
            else
            {
                UnityEngine.Debug.LogError("Not find CLRBinding logic!!!");
            }

            //注册
            ILRuntimeHelper.LoadHotfix(dllpath, mainProjectIlrBindAction, false);
            BindingCodeGenerator.GenerateBindingCode(ILRuntimeHelper.AppDomain, outputPath);
            ILRuntimeHelper.Dispose();


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
                        UnityEngine.Debug.Log("移除[已经绑定]:" + line);
                        break;
                    }
                }
            }

            //写入
            FileHelper.WriteAllLines(analysisClrBinding, analysisContent.ToArray());


            AssetDatabase.Refresh();
        }


        static List<Type> preBindingTypes = new List<Type>();

        /// <summary>
        /// 黑名单
        /// </summary>
        static List<Type> blackTypeList = new List<Type>() { typeof(UnityEngine.UI.GraphicRebuildTracker), typeof(UnityEngine.UI.Graphic), typeof(UnityEngine.UI.DefaultControls) };

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
            FileHelper.WriteAllText(prebinding, newContent);
            File.Delete(clrbinding);
        }
    }
}