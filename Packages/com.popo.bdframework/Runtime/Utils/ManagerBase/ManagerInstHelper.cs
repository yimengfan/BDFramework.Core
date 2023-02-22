using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


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
        static List<IMgr> mgrList = new List<IMgr>();

        /// <summary>
        /// 加载管理器 实例
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        static public void Load(IEnumerable<Type> types)
        {
            BDebug.LogWatchBegin("主工程管理器");
            //管理器列表
            foreach (var type in types)
            {
                if (type != null && type.IsClass && (!type.IsAbstract) && typeof(IMgr).IsAssignableFrom(type))
                {
                    // BDebug.Log("[main]加载管理器-" + type, "green");
                    var inst = type.BaseType.GetProperty("Inst", BindingFlags.Static | BindingFlags.Public);
                    if (inst != null)
                    {
                        var mgr = inst.GetValue(null, null) as IMgr;
                        if (mgr != null)
                        {
                            mgrList.Add(mgr);
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
            mgrList.Sort((a, b) =>
            {
                var aAttr = a.GetType().GetCustomAttribute<ManagerOrder>(false);
                var bAttr = a.GetType().GetCustomAttribute<ManagerOrder>(false);
                var aOrder = aAttr == null ? 0 : aAttr.Order;
                var bOrder = bAttr == null ? 0 : bAttr.Order;
                //对比
                return aOrder.CompareTo(bOrder);
            });


            //遍历type执行逻辑
            foreach (var type in types)
            {
                if (type != null && type.IsClass)
                {
                    var mgrAttributes = type.GetCustomAttributes<ManagerAttribute>(false).ToArray();
                    if (mgrAttributes != null)
                    {
                        //注册类型
                        foreach (var mgr in mgrList)
                        {
                            var ret = mgr.CheckType(type, mgrAttributes);
                            if (ret)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            BDebug.LogWatchEnd("主工程管理器");

            //管理器初始化
            foreach (var mgr in mgrList)
            {
                mgr.Init();
            }
        }


        /// <summary>
        /// 启动所有管理器
        /// </summary>
        static public void Start()
        {
            foreach (var mgr in mgrList)
            {
                if (!mgr.IsStarted)
                {
                    mgr.Start();
                }
            }
        }

        /// <summary>
        /// 开始某个具体管理器逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        static public void Start<T>()
        {
            var mgr = mgrList.FirstOrDefault((m) => m is T);

            if (mgr != null && !mgr.IsStarted)
            {
                mgr.Start();
            }
        }
    }
}
