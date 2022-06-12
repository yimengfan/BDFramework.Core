using System;
using System.Collections.Generic;
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
        static public void Load(Type[] types)
        {
            //管理器列表

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
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
            for (int i = 0; i < types.Length; i++)
            {
                var type          = types[i];
                var mgrAttributes = type.GetCustomAttributes<ManagerAttribute>(false);
                if (mgrAttributes == null)
                {
                    continue;
                }

                foreach (var mgrAttribute in mgrAttributes)
                {
                    //注册类型
                    foreach (var mgr in mgrList)
                    {
                        mgr.CheckType(type, mgrAttribute);
                    }
                }
            }

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
                mgr.Start();
            }
        }
    }
}
