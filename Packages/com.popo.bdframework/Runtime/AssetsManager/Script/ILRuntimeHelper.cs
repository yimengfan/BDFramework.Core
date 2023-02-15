using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Sql;
using ILRuntime.Mono.Cecil.Pdb;
using ILRuntime.Runtime.Generated;
using LitJson;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;


namespace BDFramework
{
    static public class ILRuntimeHelper
    {
        public static AppDomain AppDomain { get; private set; }
        public static bool IsRunning { get; private set; } = false;

        static private FileStream fsDll = null;
        static private FileStream fsPdb = null;


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
            BDebug.Log("DLL加载路径:" + dllPath, "red");
            string pdbPath = dllPath + ".pdb";
            //按需jit
            //AppDomain = new AppDomain(ILRuntimeJITFlags.JITOnDemand);
            AppDomain = new AppDomain();
            if (File.Exists(pdbPath))
            {
                //这里的流不能释放，头铁的老哥别试了
                fsDll = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
                fsPdb = new FileStream(pdbPath, FileMode.Open, FileAccess.Read);
                AppDomain.LoadAssembly(fsDll, fsPdb, new PdbReaderProvider());
            }
            else
            {
                //这里的流不能释放，头铁的老哥别试了
                fsDll = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
                AppDomain.LoadAssembly(fsDll);
            }


            //其他模块的binding，后注册的相同函数签名 会被跳过
            JsonMapper.RegisterCLRRedirection(AppDomain);
            SqliteHelper.RegisterCLRRedirection(AppDomain);
            ValueListenerEX_ILRuntimeAdaptor.RegisterCLRRedirection(AppDomain);
            EventListenerEx_ILRuntimeAdaptor.RegisterCLRRedirection(AppDomain);
            //clrbinding
            gamelogicBind?.Invoke(isDoCLRBinding);
            //开启debuger
            if (BDLauncher.Inst != null && BDLauncher.Inst.Config.IsDebuggerILRuntime)
            {
#if DEBUG
                AppDomain.UnityMainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
                AppDomain.DebugService.StartDebugService(56000);
                Debug.Log("[ILRuntime]调试端口:56000");
            }
        }

        /// <summary>
        /// ILRuntime卸载
        /// </summary>
        public static void Dispose()
        {
            AppDomain?.Dispose();
            
            if (fsDll != null)
            {
                fsDll.Close();
                fsDll.Dispose();
                fsDll = null;
            }

            if (fsPdb != null)
            {
                fsPdb.Close();
                fsPdb.Dispose();
                fsPdb = null;
            }

            AppDomain = null;
            IsRunning = false;
        }

        #region hotfix类型

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
                var values = AppDomain.LoadedTypes.Values.ToList();
                foreach (var v in values)
                {
                    hotfixTypeList.Add(v.ReflectionType);
                }
            }

            return hotfixTypeList;
        }

        #endregion


        /// <summary>
        /// 所有UIComponent的类型
        /// </summary>
        public static Dictionary<string, Type> UIComponentTypes { get; set; } = new Dictionary<string, Type>();

        #region 辅助工具类

        /// <summary>
        /// 打印堆栈
        /// </summary>
        static public void LogStackTrace()
        {
            // ILRuntimeHelper.AppDomain.DebugService.GetStackTrace()
        }


        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="value_type"></param>
        /// <returns></returns>
        static public object CreateInstance(Type value_type)
        {
            object instance = null;
            if (value_type is ILRuntime.Reflection.ILRuntimeType ilrType)
            {
                instance = ilrType.ILType.Instantiate();
            }
            else if (value_type is ILRuntime.Reflection.ILRuntimeWrapperType ilrWrapperType)
            {
                instance = Activator.CreateInstance(ilrWrapperType.RealType);
            }
            else
            {
                instance = Activator.CreateInstance(value_type);
            }

            return instance;
        }

        #endregion
    }
}
