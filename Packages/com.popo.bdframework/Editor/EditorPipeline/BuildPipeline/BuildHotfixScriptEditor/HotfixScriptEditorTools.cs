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
        static public void RoslynBuild(string outpath, RuntimePlatform platform, Unity3dRoslynBuildTools.BuildMode mode, bool isShowTips = true)
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
            Unity3dRoslynBuildTools.BuildDll(outpath, platform, mode, isShowTips);

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


    }
}