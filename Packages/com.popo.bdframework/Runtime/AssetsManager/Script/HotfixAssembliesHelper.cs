using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;


namespace BDFramework
{
    static public class HotfixAssembliesHelper
    {
        public static bool IsRunning { get; private set; } = false;


        /// <summary>
        /// 热更dll
        /// </summary>
        static private List<Assembly> hotfixAssemblyList { get; set; } = new List<Assembly>();


        /// <summary>
        /// 加载Hotfix程序集
        /// </summary>
        /// <param name="dllPath"></param>
        /// <param name="gamelogicBind">游戏逻辑测注册</param>
        /// <param name="isDoCLRBinding"></param>
        public static void LoadHotfix(string dllPath, Action<bool> gamelogicBind = null, bool isDoCLRBinding = true)
        {
            //
            IsRunning = true;
            BDebug.Log("DLL加载路径:" + dllPath, Color.red);
            string pdbPath = dllPath + ".pdb";
            //按需jit
            var fsbytes = File.ReadAllBytes(dllPath);
            var assmbly = Assembly.Load(fsbytes);
            hotfixAssemblyList.Add(assmbly);

            //
            gamelogicBind?.Invoke(isDoCLRBinding);
        }

        /// <summary>
        /// ILRuntime卸载
        /// </summary>
        public static void Dispose()
        {
            //AppDomain?.Dispose();

            IsRunning = false;
        }


        /// <summary>
        /// 获取主入口assembly
        /// </summary>
        /// <returns></returns>
        static public Assembly GetMainAssembly()
        {
           return hotfixAssemblyList[0];
        }



        static private List<Type> hotfixTypeList = null;

        /// <summary>
        /// 获取所有的hotfix的类型
        /// </summary>
        /// <returns></returns>
        public static List<Type> GetHotfixTypes()
        {
            if (hotfixTypeList == null)
            {
                hotfixTypeList = new List<Type>();

                foreach (var assembly in hotfixAssemblyList)
                {
                    var types = assembly.GetTypes();

                    hotfixTypeList.AddRange(types);
                }
            }

            return hotfixTypeList;
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
            //     instance = ilrType.ILType.Instantiate();
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
    }
}
