using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace BDFramework.Mgr
{
    /// <summary>
    /// 主工程管理器工具
    /// </summary>
    static public class ManagerInstHelper
    {
        /// <summary>
        /// mgr列表
        /// </summary>
        private static List<IMgr> MgrList { get;  set; } = new List<IMgr>();

        /// <summary>
        /// 获取需要搜集的Class
        /// </summary>
        /// <returns></returns>
        static public Type[] GetMainProjectTypes()
        {
            BDebug.LogWatchBegin("加载所有DLL-types");
            var typeList = new List<Type>();
            Assembly[] assemblyList = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblyList)
            {
                //只搜集以下DLLType
                if (assembly.FullName.StartsWith("BDFramework") //框架相关的类
                    || assembly.FullName.StartsWith("Assembly-CSharp,") //unity未定义Assembly的class
                    || assembly.FullName.StartsWith("Assembly-CSharp-firstpass,") //unity未定义Standard Assets的class
                    || assembly.FullName.StartsWith("UnityEngine.UI") //UnityUI类
                    || assembly.FullName.StartsWith("Game.") //所有以Game.开头定义的Assembly,可以定义AssemblyDefine以该字符开头则会被收集
                    || assembly.FullName.Contains("@main") //所有包含@main的Assembly,可以定义AssemblyDefine以该字符开头则会被收集
                   )
                {
                    var ts = assembly.GetTypes().Where((t) => t != null && t.IsClass && !t.IsNested);
                    typeList.AddRange(ts);
                }
            }

#if UNITY_EDITOR
            typeList.Sort((a, b) => a.FullName.CompareTo(b.FullName));
#endif
            var types = typeList.ToArray();
            BDebug.LogWatchEnd("加载所有DLL-types");
            return types;
        }


        /// <summary>
        /// 加载管理器 实例
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        static public List<string> LoadManager(IEnumerable<Type> types, IEnumerable<string> exsitMgrNames = null)
        {
            List<string> replacedMgrNames = new List<string>();
            //搜集管理器
            foreach (var type in types)
            {
                if (type != null && type.IsClass && (!type.IsAbstract) && typeof(IMgr).IsAssignableFrom(type))
                {
                    if (exsitMgrNames != null && exsitMgrNames.Contains(type.Name))
                    {
                        BDebug.Log($"热更存在Mgr,由热更接管->{type.Name}", Color.magenta);
                        replacedMgrNames.Add(type.Name);
                        continue;
                    }


                    // BDebug.Log("[main]加载管理器-" + type, Color.green);
                    var inst = type.BaseType.GetProperty("Inst", BindingFlags.Static | BindingFlags.Public);
                    if (inst != null)
                    {
                        var mgr = inst.GetValue(null, null) as IMgr;
                        if (mgr != null)
                        {
                            MgrList.Add(mgr);
                            if (Application.isPlaying)
                            {
                                BDebug.Log("[main] 加载管理器-" + type.FullName, Color.yellow);
                            }
                        }
                        else
                        {
                            BDebug.LogError("加载管理器失败,-" + type);
                        }
                    }
                    else
                    {
                        BDebug.LogError("加载管理器失败,-" + type);
                    }
                }
            }

            //按执行顺序排序
            MgrList.Sort((a, b) =>
            {
                var aAttr = a.GetType().GetCustomAttribute<ManagerOrder>(false);
                var bAttr = a.GetType().GetCustomAttribute<ManagerOrder>(false);
                var aOrder = aAttr == null ? 0 : aAttr.Order;
                var bOrder = bAttr == null ? 0 : bAttr.Order;
                //对比
                return aOrder.CompareTo(bOrder);
            });


            //注册类型
            RegisterType(types);


            return replacedMgrNames;
        }


        /// <summary>
        /// 注册类型
        /// </summary>
        /// <param name="types"></param>
        static public void RegisterType(IEnumerable<Type> types, bool isHotfixType = false)
        {
            //遍历type执行逻辑
            foreach (var type in types)
            {
                if (type != null && type.IsClass)
                {
                    var mgrAttributes = type.GetCustomAttributes<ManagerAttribute>(false).ToArray();
                    if (mgrAttributes != null)
                    {
                        //注册类型
                        foreach (var mgr in MgrList)
                        {
                            bool ret = false;
                            if (!isHotfixType)
                            {
                                ret = mgr.RegisterTypes(type, mgrAttributes);
                            }
                            else
                            {
                                ret = mgr.RegisterHotfixTypes(type, mgrAttributes);
                            }

                            if (ret)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 启动所有管理器
        /// </summary>
        static public void Start()
        {
            //管理器初始化
            foreach (var mgr in MgrList)
            {
                mgr.Init();
            }

            //管理器启动
            foreach (var mgr in MgrList)
            {
                if (!mgr.IsStarted)
                {
                    mgr.Start();
                    BDebug.Log($"[main] 主工程管理器启动 {mgr.GetType().Name}", Color.red);
                }
            }
        }

        /// <summary>
        /// 开始某个具体管理器逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void Start<T>()
        {
            var mgr = MgrList.FirstOrDefault((m) => m is T);

            if (mgr != null && !mgr.IsStarted)
            {
                mgr.Start();
            }
        }
    }
}
