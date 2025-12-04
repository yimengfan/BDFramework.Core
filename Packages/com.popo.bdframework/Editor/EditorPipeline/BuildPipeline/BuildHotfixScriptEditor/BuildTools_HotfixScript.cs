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
// using ILRuntime.Runtime.CLRBinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// 热更代码工具
    /// </summary>
    static public class BuildTools_HotfixScript
    {
        static string ENABLE_ILRUNTIME = "ENABLE_ILRUNTIME";
        static private string ENABLE_HYCLR = "ENABLE_HYCLR";



        /// <summary>
        /// 设置hyclr的config
        /// </summary>
        static public void SetHyCLRConfig()
        {
            Unity3dEditorEx.AddSymbols(ENABLE_HYCLR);
            HyCLREditorTools.SetBDFramework2HCLRConfig();
        }


        /// <summary>
        /// 编译DLL
        /// 使用Roslyn编译
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="platform"></param>
        /// <param name="mode"></param>
        static public void BuildDLL(string outpath, RuntimePlatform platform)
        {
            //触发bd环境周期
            BDFrameworkPipelineHelper.OnBeginBuildHotfixDLL();
            //开始构建热更dll
            var buildTarget = BApplication.GetBuildTarget(platform);
            HyCLREditorTools.BuildHotfixDLL(outpath, buildTarget);
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