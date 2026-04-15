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
    /// 脚本加载器
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
        /// 初始化整个热更代码
        /// </summary>
        [Preserve]
        public static void Init()
        {
            //list
            var types = ScriptLoder.GetAppDomainHostingTypes();
            //加载主工程的管理器
            ManagerInstHelper.LoadManager(types);
            GameConfigLoder.LoadFrameworkConfig();

            //启动E2E
            // 当 Talos.E2E 包不存在或非 Debug 构建时，此调用安全无副作用。
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

            Debug.Log($"框架托管DLL:{string.Join("\n", typeList.Select(t => t.FullName))}");

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

         /// <summary>        /// /// 
         /// 
         /// 这个只能在 debug 模式下使用！！！！！！
        /// 在热更 DLL 加载 + 框架初始化完成后调用，确保 E2E 测试可以正确发现热更类型。
        /// 如果 Talos.E2E 包不存在，则静默跳过。
        /// 是否真正启动 E2E 由 Talos.E2E.E2EAutoInit 在运行时继续根据 DEBUG 标记文件或 -talosForceE2E 参数判定。
        /// Editor 和真机统一走此入口：
        /// - 真机：E2EAutoInit → LaunchE2E（MonoBehaviour 模式）
        /// - Editor PlayMode：E2EAutoInit → LaunchE2EStatic（静态模式，由 DidReloadScripts 管理）
        /// - Editor 非进 PlayMode：由 LaunchE2EEditorOnly 直接启动静态 TCP，不经此路径
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
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