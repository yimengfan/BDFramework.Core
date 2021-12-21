using System;
using System.Collections.Generic;
using System.Reflection;


namespace BDFramework.Mgr
{
    /// <summary>
    /// 管理器单例助手
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
                if (type != null  && type.IsClass&& typeof(IMgr).IsAssignableFrom(type))
                {
                    BDebug.Log("[main]加载管理器-" + type, "green");
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

            //遍历type执行逻辑
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var mgrAttribute = type.GetCustomAttribute<ManagerAttribute>();
                if (mgrAttribute == null)
                {
                    continue;
                }
                //注册类型
                foreach (var iMgr in mgrList)
                {
                    iMgr.CheckType(type, mgrAttribute);
                }
            }


            //管理器初始化
            foreach (var m in mgrList)
            {
                m.Init();
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