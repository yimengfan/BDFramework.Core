using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.Editor.Environment;
using BDFramework.GameStart;
// using ILRuntime.Runtime.CLRBinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// ILRuntime 编辑器工具
    /// </summary>
   static public class ILRuntimeEditorTools
    {
        
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
                dllpath = BApplication.streamingAssetsPath;
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
            // ILRuntimeHelper.LoadHotfix(dllpath, mainProjectIlrBindAction, false);
            // BindingCodeGenerator.GenerateBindingCode(ILRuntimeHelper.AppDomain, outputPath);
            // ILRuntimeHelper.Dispose();


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
            // BindingCodeGenerator.GenerateBindingCode(preBindingTypes, output, excludeMethods: blackMethodList);
            var oldContent = File.ReadAllText(clrbinding);
            var newContent = oldContent.Replace("class CLRBindings", "class PreCLRBinding");
            //写入新的,删除老的
            FileHelper.WriteAllText(prebinding, newContent);
            File.Delete(clrbinding);
        }
    }
}