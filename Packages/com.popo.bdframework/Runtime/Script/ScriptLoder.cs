using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Configure;
using BDFramework.Mgr;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework
{
    /// <summary>
    /// 框架脚本加载器。
    /// Framework script loader.
    /// 负责收集托管类型、注册管理器、加载框架配置，并在初始化末尾桥接可选的 Talos E2E 自动发现入口。
    /// It collects hosted types, registers managers, loads framework configuration, and bridges the optional Talos E2E auto-discovery entry at the end of initialization.
    /// </summary>
    static public class ScriptLoder
    {
        private static readonly string Tag = "ScriptLoder";

        /// <summary>
        /// aot patch路径
        /// </summary>
        static readonly public string HYCLR_AOT_PATCH_PATH = $"script/aot_patch";

        /// <summary>
        /// 热更dll定义
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix";

        /// <summary>
        /// 热更代码后缀
        /// </summary>
        static readonly public string HOT_DLL_EXTENSION = ".zlua.bytes";


        /// <summary>
        /// 初始化框架脚本与管理器入口。
        /// Initialize the framework script and manager entrypoints.
        /// 该入口会先收集托管类型并完成管理器与配置初始化，随后桥接 Talos E2E 自动检测，
        /// 以保证运行时与真机环境都能发现可用的测试入口而不依赖编译期 DEBUG 裁剪。
        /// This entry first collects hosted types and completes manager and configuration initialization, then bridges Talos E2E auto-detection,
        /// so runtime and packaged-player environments can discover available test entrypoints without relying on compile-time DEBUG stripping.
        /// </summary>
        [Preserve]
        public static void Init()
        {
            // 收集当前应用域内由框架托管的类型集合。
            // Collect the set of hosted types managed by the framework in the current AppDomain.
            var types = ScriptLoder.GetAppDomainHostingTypes();
            // 加载主工程管理器并在其后完成框架配置初始化。
            // Load the main-project managers and then complete framework-configuration initialization.
            ManagerInstHelper.LoadManager(types);
            GameConfigLoder.LoadFrameworkConfig();

            // 在主线程预热 BApplication，避免后台线程先触发静态构造时访问 Application.dataPath 并把资源/SQLite 相关路径状态永久污染。
            // Warm up BApplication on the main thread so a background thread cannot trigger its static constructor first, touch Application.dataPath, and permanently poison later resource and SQLite path state.
            _ = BDFramework.Core.Tools.BApplication.persistentDataPath;

            // 标记脚本加载器为运行态，表示热更 DLL 加载和管理器注册已完成。
            // Mark the script loader as running, indicating hotfix DLL loading and manager registration have completed.
            IsRunning = true;
        }

        /// <summary>
        /// 启动框架管理器并标记脚本加载器为运行态。
        /// Start framework managers and mark the script loader as running.
        /// 调用此方法后 <c>IsRunning</c> 将为 <c>true</c>，表示热更 DLL 加载与管理器初始化已完成。
        /// After calling this method <c>IsRunning</c> will be <c>true</c>, indicating that hotfix DLL loading and manager initialization have completed.
        /// </summary>
        /// <param name="mainProjectTypes"></param>
        static public void Start()
        {
            // 启动管理器实例。
            // Start manager instances.
            ManagerInstHelper.Start();
            // 标记脚本加载器为运行态，表示热更 DLL 加载和管理器初始化已完成。
            // Mark the script loader as running, indicating hotfix DLL loading and manager initialization have completed.
            IsRunning = true;
        }

        #region 托管 type

        public static bool IsRunning { get; private set; } = false;


        /// <summary>
        /// ILRuntime卸载
        /// </summary>
        public static void Dispose()
        {
            //AppDomain?.Dispose();

            IsRunning = false;
        }


        /// <summary>
        /// 托管类型
        /// </summary>

        static private List<Type> hostingTypeList { get; set; } = null;


        /// <summary>
        /// 获取当前程序域中由框架托管的所有类型。
        /// Get all framework-hosted types from the current application domain.
        /// 该方法会缓存第一次扫描结果，后续调用直接复用缓存，避免重复遍历程序集。
        /// The method caches the first scan result and reuses it on later calls to avoid rescanning assemblies.
        /// </summary>
        /// <returns></returns>
        static public IEnumerable<Type> GetAppDomainHostingTypes()
        {
            if (hostingTypeList != null)
            {
                return hostingTypeList;
            }

            var typeList = new List<Type>();
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies();

            BDebug.LogWatchBegin("加载所有DLL-types");
            foreach (var assembly in assemblyList)
            {
                //只搜集以下DLLType
                if (
                    //框架
                    assembly.FullName.StartsWith("BDFramework") //框架相关的类
                    //默认 class
                    || assembly.FullName.StartsWith("Assembly-CSharp,") //unity未定义Assembly的class
                    || assembly.FullName.StartsWith("Assembly-CSharp-firstpass,") //unity未定义Standard Assets的class
                    //引擎相关
                    || assembly.FullName.StartsWith("UnityEngine.UI") //UnityUI类
                    //游戏业务
                    || assembly.FullName.StartsWith("Game.") //所有以Game.开头定义的Assembly,可以定义AssemblyDefine以该字符开头则会被收集
                    || assembly.FullName.Contains("@main") //所有包含@main的Assembly,可以定义AssemblyDefine以该字符开头则会被收集
                )
                {
                    var ts = assembly.GetTypes().Where((t) => t != null && t.IsClass && !t.IsNested);
                    typeList.AddRange(ts);
                }
            }

            UnityEngine.Debug.Log($"框架托管DLL:{string.Join("\n", typeList.Select(t => t.FullName))}");

#if UNITY_EDITOR
            typeList.Sort((a, b) => a.FullName.CompareTo(b.FullName));
#endif
            hostingTypeList = typeList;
            BDebug.LogWatchEnd("加载所有DLL-types");
            return typeList;
        }


        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="value_type"></param>
        /// <returns></returns>
        static public object CreateHotfixInstance(Type value_type)
        {
            object instance = null;
            // if (value_type is ILRuntime.Reflection.ILRuntimeType ilrType)
            // {
            //     instance = ilrType.ILType.I334nstantiate();
            // }
            // else if (value_type is ILRuntime.Reflection.ILRuntimeWrapperType ilrWrapperType)
            // {
            //     instance = Activator.CreateInstance(ilrWrapperType.RealType);
            // }
            // else
            {
                instance = Activator.CreateInstance(value_type);
            }

            return instance;
        }

        #endregion

    }
}
