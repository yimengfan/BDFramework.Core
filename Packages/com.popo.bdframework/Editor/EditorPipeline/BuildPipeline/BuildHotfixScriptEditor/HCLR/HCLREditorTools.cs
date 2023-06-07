#if ENABLE_HCLR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.Tools;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using LitJson;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.HotfixScript
{
    /// <summary>
    /// HCLR 编辑器工具
    /// </summary>
    static public class HCLREditorTools
    {
        //
        static string libil2cppPath = "HybridCLRData/iOSBuild/build/libil2cpp.a";
        // /// <summary>
        // /// 测试
        // /// </summary>
        //[MenuItem("xxx")]
        static public void Test()
        {
            //PreBuild(BuildTarget.Android);


            PreBuild(BuildTarget.Android,BApplication.DevOpsPublishAssetsPath);
        }
        //
        /// <summary>
        /// 在打包前执行
        /// </summary>
        /// <param name="target"></param>
        static public void PreBuild( BuildTarget target,string assetsOutputDir)
        {
            if (HybridCLRSettings.Instance == null)
            {
                throw new Exception("请先生成HCLR Setting!");
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            Debug.Log("<color=green>[HCLR]start:</color>");
            SetBDFramework2HCLRConfig();
            //编译补充元数据的DLL
            Debug.Log("<color=green>[HCLR]gen aot dll!</color>");
            CompileDllCommand.CompileDll(target);
            CopyCompileDll(target,assetsOutputDir);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            
            // 这几个生成依赖HotUpdateDlls
            Debug.Log("<color=green>[HCLR]gen link.xml!</color>");
            LinkGeneratorCommand.GenerateLinkXml(target);
            
            // 生成裁剪后的aot dll
            Debug.Log("<color=green>[HCLR]gen strip aot dll!</color>");
            if (IsNeedStripAOTDll(target,assetsOutputDir))
            {
                StripAOTDllCommand.GenerateStripedAOTDlls(target,  BApplication.GetBuildTargetGroup(target));
                CopyStripAOTDll(target,assetsOutputDir);
            }
            else
            {
                Debug.Log($"<color=yellow>[HCLR]已存在{JsonMapper.ToJson(HybridCLRSettings.Instance.patchAOTAssemblies)}, 不需要 gen strip aot dll !!!</color>");
            }

            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            Debug.Log("<color=green>[HCLR]gen bridge cpp!</color>");
            MethodBridgeGeneratorCommand.GenerateMethodBridge(target);
            ReversePInvokeWrapperGeneratorCommand.GenerateReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
            
            sw.Stop();
            Debug.Log($"<color=red>[HCLR]end! 耗时:{sw.ElapsedMilliseconds} ms.</color>");
#if UNITY_EDITOR_OSX
            BuildLibIl2cppForIOS();
#endif
        }

        /// <summary>
        /// 设置HCLR配置
        /// </summary>
        static public void SetBDFramework2HCLRConfig()
        {
            //HCLR Setting
            //BD的hotfix dll
            {
                var list = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies);
                if (!list.Contains(ScriptLoder.HOTFIX_DEFINE))
                {
                    list.Add(ScriptLoder.HOTFIX_DEFINE);
                    HybridCLRSettings.Instance.hotUpdateAssemblies = list.ToArray();
                }
            }
            //BD框架的搜索目录
            {
                var list = new List<string>( HybridCLRSettings.Instance.externalHotUpdateAssembliyDirs);
                foreach (var platform in BApplication.SupportPlatform)
                {
                    var scriptPath = IPath.Combine( BApplication.GetPlatformDevOpsPublishAssetsPath(platform), ScriptLoder.SCRIPT_FOLDER_PATH);
                    scriptPath = IPath.ReplaceBackSlash(scriptPath);
                    if (!list.Contains(scriptPath))
                    {
                        list.Add(scriptPath);
                    }
                }
                
                HybridCLRSettings.Instance.externalHotUpdateAssembliyDirs = list.ToArray();
            }
            
            //Patch AOT
            {
                string[] aotAssemblies = new string[] { "mscorlib", "System", "System.Core" };
                var list = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies);
                foreach (var aotAssembly in aotAssemblies)
                {
                    if (!list.Contains(aotAssembly))
                    {
                        list.Add(aotAssembly);
                    }
                }
                HybridCLRSettings.Instance.patchAOTAssemblies = list.ToArray();
            } 
            
            //保存
            HybridCLRSettings.Save();

        }

        /// <summary>
        /// 构建libil2cpp.so
        /// </summary>
        /// <exception cref="Exception"></exception>
        static public void BuildLibIl2cppForIOS()
        {
            if (Directory.Exists("HybridCLRData/iOSBuild/build"))
            {
                Directory.Delete("HybridCLRData/iOSBuild/build",true);
            }
            
            var shPath = "HybridCLRData/iOSBuild/build_libil2cpp.sh";
            if (File.Exists(shPath))
            {
                var cmds = new string[]
                {
                    $"cd {BApplication.ProjectRoot}/HybridCLRData/iOSBuild",
                    "bash ./build_libil2cpp.sh"
                };
                CMDTools.RunCmd(cmds,islog:false);
            }
            else
            {
                throw new Exception("请编译libil2cpp.so for HCLR!!!");
            }
            
            //校验结果
          
            if (!File.Exists(libil2cppPath))
            {
                throw new Exception($"编译libil2cpp.a 失败！！! [{libil2cppPath}]"); 
            }
            else
            {
                Debug.Log("<color=yellow>构建libil2cpp.a成功</color> :" + libil2cppPath);
            }
        }


        /// <summary>
        /// 拷贝Libil2cpp.a
        /// </summary>
        static public void CopyLibIl2cppToXcode(string destDirectPath)
        {
            if (File.Exists(libil2cppPath))
            {
                var destFilePath = IPath.Combine(destDirectPath,Path.GetFileName(libil2cppPath));
                File.Copy(libil2cppPath,destFilePath , true);
                BDebug.Log("<color=green>[HCLR] 拷贝libil2cpp.a 成功!</color>");
            }
            else
            {
                throw new Exception("拷贝libil2cpp.a 失败!");
            }
        }


        
        /// <summary>
        /// 拷贝补充元数据的dll到hotfix目录
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assetsOutputDir"></param>
        static public void CopyCompileDll(BuildTarget target,string assetsOutputDir)
        {
            //从aot目录拷贝补充元数据的dll到hotfix目录
            var patchAOTDll = HybridCLRSettings.Instance.patchAOTAssemblies;
            assetsOutputDir = IPath.Combine(assetsOutputDir, BApplication.GetPlatformPath(target));
            foreach (var dll in patchAOTDll)
            {
                var sourceDllPath = IPath.Combine(  SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), dll + ".dll");
                var destPath = IPath.Combine(assetsOutputDir,ScriptLoder.HCLR_AOT_PATCH_PATH, dll + ".dll");
                if (File.Exists(sourceDllPath))
                {
                    FileHelper.Copy(sourceDllPath,destPath,true);
                    Debug.Log($"<color=green>[HCLR]拷贝AOT Patch dll:{dll} </color>");
                }
            }
        }


        /// <summary>
        /// 是否需要拷贝StripDll
        /// </summary>
        static public bool IsNeedStripAOTDll(BuildTarget target,string assetsOutputDir)
        {
            var patchAOTDlls = HybridCLRSettings.Instance.patchAOTAssemblies;
            assetsOutputDir = IPath.Combine(assetsOutputDir, BApplication.GetPlatformPath(target));
            foreach (var dll in patchAOTDlls)
            {
                var destPath = IPath.Combine(assetsOutputDir,ScriptLoder.HCLR_AOT_PATCH_PATH, dll + ".dll");
                if (!File.Exists(destPath))
                {
                    Debug.Log($"<color=red>[HCLR]不存在AOT Patch dll:{destPath},需要进行strip aot build dll!</color>");
                    return true;
                }
            }
          
            return false;
        }
        /// <summary>
        /// 拷贝补充元数据的dll到hotfix目录
        /// </summary>
        /// <param name="target"></param>
        /// <param name="assetsOutputDir"></param>
        static public void CopyStripAOTDll(BuildTarget target,string assetsOutputDir)
        {
            //从aot目录拷贝补充元数据的dll到hotfix目录
            var patchAOTDll = HybridCLRSettings.Instance.patchAOTAssemblies;
            assetsOutputDir = IPath.Combine(assetsOutputDir, BApplication.GetPlatformPath(target));
            foreach (var dll in patchAOTDll)
            {
                var sourceDllPath = IPath.Combine(  SettingsUtil.GetAssembliesPostIl2CppStripDir(target), dll + ".dll");
                var destPath = IPath.Combine(assetsOutputDir,ScriptLoder.HCLR_AOT_PATCH_PATH, dll + ".dll");
                if (File.Exists(sourceDllPath))
                {
                    FileHelper.Copy(sourceDllPath,destPath,true);
                    Debug.Log($"<color=green>[HCLR]拷贝Strip AOT Patch dll:{dll} </color>");
                }
            }

        }
        
    }
}
#endif