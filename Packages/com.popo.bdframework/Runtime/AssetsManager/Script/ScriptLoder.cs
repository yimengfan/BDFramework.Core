using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.Mgr;
using Cysharp.Threading.Tasks;
using UnityEngine;


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
        /// 初始化整个热更域
        /// </summary>
        static void Init()
        {
            //list
            var types = ScriptLoder.GetHostingTypes();
            //启动主工程的管理器
            ManagerInstHelper.LoadManager(types);
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
        /// 获取框架托管的所有类型
        /// </summary>
        /// <returns></returns>
        static public IEnumerable<Type> GetHostingTypes()
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

            Debug.Log($"框架托管DLL:{string.Join(",", typeList.Select(t => t.FullName))}");

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
    }
}