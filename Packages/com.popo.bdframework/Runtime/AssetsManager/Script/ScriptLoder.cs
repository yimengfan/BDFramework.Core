using System;
using System.IO;
using System.Reflection;
using BDFramework.Configure;
using BDFramework.Core.Tools;

using UnityEngine;
#if ENABLE_HYCLR
using HybridCLR;
#endif

namespace BDFramework
{
    /// <summary>
    /// 脚本加载器
    /// </summary>
    static public class ScriptLoder
    {
        private static readonly string Tag = "ScriptLoder";


        /// <summary>
        /// aot patch路径
        /// </summary>
        static readonly public string HCLR_AOT_PATCH_PATH = $"script/aot_patch_dll";

        /// <summary>
        /// 热更dll定义
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix_dll";
        



        /// <summary>
        /// 脚本加载入口
        /// </summary>
        /// <param name="loadPathType"></param>
        /// <param name="runMode"></param>
        /// <param name="mainProjectTypes">UPM隔离了dll,需要手动传入</param>
        static public void Init(AssetLoadPathType loadPathType, HotfixCodeRunMode runMode, Type[] mainProjectTypes)
        {
            BDebug.EnableLog(Tag);
            // CLRBindAction = clrBindingAction;

            if (loadPathType == AssetLoadPathType.Editor)
            {
                BDebug.Log(Tag, "Editor(非热更)模式!");
                //反射调用，防止编译报错
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                //添加框架部分的type，热更下不需要，打包会把框架的部分打进去
                // var list = new List<Type>();
                // list.AddRange(mainProjectTypes);
                // list.AddRange(typeof(BDLauncher).Assembly.GetTypes());
                method.Invoke(null, new object[] {mainProjectTypes, null});
            }
            else
            {
                BDebug.Log(Tag, "热更模式!");
                LoadHotfixDLL(loadPathType, runMode, mainProjectTypes);
            }
        }


        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="source"></param>
        /// <param name="copyto"></param>
        /// <returns></returns>
        static public void LoadHotfixDLL(AssetLoadPathType loadPathType, HotfixCodeRunMode mode, Type[] mainProjecTypes)
        {
            //路径
         
            //加载元数据
            if (mode == HotfixCodeRunMode.HyCLR)
            {
                if (!Application.isEditor)
                {
                    //--------------------元数据DLL加载------------------------
                    //加载AOT,AOT Pacth 一定在母包Streaming内
                    var aotPatchRoot = Path.Combine(BApplication.GetRuntimePlatformPath(), HCLR_AOT_PATCH_PATH);
                    BDebug.Log($"---------------【ScriptLoder】HCLR执行, Dll路径:{aotPatchRoot}---------------"  , Color.red);
                    //
                    var aotPatchDlls = BetterStreamingAssets.GetFiles(aotPatchRoot, "*.dll.bytes");
                    foreach (var path in aotPatchDlls)
                    {
                        BDebug.Log("【ScriptLoder】HCLR加载AOT Patch:" + path, Color.yellow);
                        var dllbytes = BetterStreamingAssets.ReadAllBytes(path);
                        var err = RuntimeApi.LoadMetadataForAOTAssembly(dllbytes, HomologousImageMode.SuperSet);
                        Debug.Log($"LoadMetadataForAOTAssembly:{path}. ret:{err}");
         
                    }
                }


                //---------------------热更DLL加载------------------------
                var hotfixdllRootPath = Path.Combine(GameBaseConfigProcessor.GetLoadPath(loadPathType), BApplication.GetRuntimePlatformPath());
                BDebug.Log($"---------------【ScriptLoder】HCLR执行, Dll路径:{hotfixdllRootPath}---------------", Color.red);
                string[] hotfixDlls = null;
                if (Directory.Exists(hotfixdllRootPath))
                {
                    hotfixDlls = Directory.GetFiles(hotfixdllRootPath, "*.dll.bytes");
                }
                
                if(hotfixDlls!=null && hotfixDlls.Length>0)
                {
                 
                    foreach (var hotfixDll in hotfixDlls)
                    {
                        var dllBytes = File.ReadAllBytes(hotfixDll);
                        BDebug.Log($"【ScriptLoder】{loadPathType} -:" + hotfixDll, Color.yellow);
                        Assembly.Load(dllBytes);
                    }
                }
                else
                {
                    //streaming加载
                    hotfixdllRootPath = Path.Combine(BApplication.GetRuntimePlatformPath(), HCLR_AOT_PATCH_PATH);
                    BDebug.Log($"---------------【ScriptLoder】重新寻址 HyCLR执行  Dll路径:{hotfixdllRootPath}---------------", Color.red);
                    hotfixDlls = BetterStreamingAssets.GetFiles(hotfixdllRootPath, "*.dll.bytes");
                    if (hotfixDlls == null || hotfixDlls.Length == 0)
                    {
                        throw new Exception("【ScriptLoder】HyCLR热更DLL不存在! 路径:" + hotfixdllRootPath);
                    }
                    //
                    foreach (var hotfixDll in hotfixDlls)
                    {
                        var dllBytes = BetterStreamingAssets.ReadAllBytes(hotfixDll);
                        BDebug.Log($"【ScriptLoder】{AssetLoadPathType.StreamingAsset} -:" + hotfixDll, Color.yellow);
                        Assembly.Load(dllBytes);
                    }
                }

  



                //---------------------启动------------------------
                Assembly assembly = null;
                var type = typeof(ScriptLoder).Assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                var startFunc = (Action<Type[], Type[]>) Delegate.CreateDelegate(typeof(Action<Type[], Type[]>), method);
                try
                {
                    var hotfixTypes = assembly.GetTypes();
                    //开始
                    startFunc(mainProjecTypes, hotfixTypes);
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var exc in e.LoaderExceptions)
                    {
                        Debug.LogError(exc);
                    }
                }
            }

            else
            {
                BDebug.Log("【ScriptLoder】Dll路径:内置", Color.magenta);
            }
        }


        /// <summary>
        /// 获取当前本地DLL
        /// </summary>
        static public string GetLocalDLLPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BApplication.GetPlatformPath(platform),HOTFIX_DLL_PATH);
        }


    }
}
