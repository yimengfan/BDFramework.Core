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

            // 桥接 E2E 自动检测入口；如果 Talos.E2E 包不存在，则该调用会在方法内部静默退出。
            // Bridge the E2E auto-detection entry; if the Talos.E2E package is absent, the method exits quietly from inside.
            TryStartE2EFramework();
        }

        /// <summary>
        /// 整个游戏的管理器
        /// </summary>
        /// <param name="mainProjectTypes"></param>
        static public void Start()
        {
            //开始
            ManagerInstHelper.Start();
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
        /// 获取程序域
        /// 框架托管的所有类型
        /// </summary>
        /// <returns></returns>
        static public IEnumerable<Type> GetAppDomainHostingTypes()
        {
            if (hostingTypeList != null)
            {
                return hostingTypeList;
            }

            BDebug.LogWatchBegin("加载所有DLL-types");
            var typeList = new List<Type>(1000);
            Assembly[] assemblyList = System.AppDomain.CurrentDomain.GetAssemblies();
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


         #region E2E 测试自动集成

         /// <summary>
         /// 尝试桥接 Talos E2E 自动检测入口。
         /// Try to bridge the Talos E2E auto-detection entrypoint.
         /// 该入口必须在 Player 与真机场景中保持运行时可达，不能再依赖 Conditional(DEBUG) 的编译期裁剪；
         /// 否则像 Windows 这样直接经由 ScriptLoder 启动的母包会静默丢失 Talos TCP 启动桥接。
         /// This entry must remain runtime-reachable in player and packaged-device scenarios and can no longer rely on Conditional(DEBUG) compile-time stripping;
         /// otherwise packaged players that boot directly through ScriptLoder, such as Windows, silently lose the Talos TCP startup bridge.
         /// 是否真正启动 E2E 仍由 Talos.E2E.E2EAutoInit 在运行时根据标记文件或 -talosForceE2E 参数继续判定。
         /// Whether E2E actually starts is still decided at runtime by Talos.E2E.E2EAutoInit based on marker files or the -talosForceE2E argument.
         /// 如果 Talos.E2E 包不存在，则静默跳过。
         /// If the Talos.E2E package is not present, the method exits quietly.
         /// </summary>
        static private void TryStartE2EFramework()
        {
            try
            {
                UnityEngine.Debug.Log("[TalosE2E] ScriptLoder.Init 阶段开始检测 E2E 自动启动入口");

                // 查找 Talos.E2E 程序集中的 E2EAutoInit 类型
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("Talos.E2E.E2EAutoInit");
                    if (type == null)
                    {
                        continue;
                    }

                    var method = type.GetMethod("CheckAndLaunch",
                        BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                    {
                        UnityEngine.Debug.LogWarning("[TalosE2E] 找到 E2EAutoInit 但无 CheckAndLaunch 方法");
                        return;
                    }

                    UnityEngine.Debug.Log($"[TalosE2E] 已解析自动启动入口 assembly={assembly.GetName().Name} port=10002");
                    method.Invoke(null, new object[] { 10002 });
                    UnityEngine.Debug.Log("[TalosE2E] ScriptLoder.Init 阶段已触发 E2E 自动检测");
                    return;
                }

                // Talos.E2E 包未安装，正常跳过
                UnityEngine.Debug.Log("[TalosE2E] 当前进程未发现 E2EAutoInit，跳过自动启动入口检测");
            }
            catch (System.Exception ex)
            {
                // E2E 自动检测失败不影响框架正常启动
                UnityEngine.Debug.LogWarning($"[TalosE2E] E2E 自动检测失败（不影响启动）: {ex.Message}");
            }
        }

        #endregion
    }
}