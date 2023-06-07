using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Hotfix.Reflection;
using BDFramework.Mgr;
using LitJson;
using UnityEngine;

namespace BDFramework.HotFix.Mgr
{
    /// <summary>
    /// 热更管理器单例工具
    /// </summary>
    static public class HotfixManagerInstHelper
    {
        static List<IMgr> hotfixMgrList = new List<IMgr>();

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="hotfixTypes"></param>
        /// <returns></returns>
        static public IMgr[] LoadManager(IEnumerable<Type> hotfixTypes)
        {
            //管理器列表
            foreach (var type in hotfixTypes)
            {
                if (type != null && type.BaseType != null && type.BaseType.FullName != null)
                {
                    if (type.BaseType.FullName.Contains(".ManagerBase`2")) //这里ILR里面只能这么做，丑但有效
                    {
                        BDebug.Log("[hotfix] 加载管理器-" + type.FullName, Color.green);
                        var inst = type.BaseType.GetProperty("Inst").GetValue(null, null);
                        var mgr = inst as IMgr;
                        if (mgr != null)
                        {
                            hotfixMgrList.Add(mgr);
                        }
                        else
                        {
                            BDebug.LogError("[hotfix]加载管理器失败-" + type.FullName);
                        }
                    }
                }
            }

            //按执行顺序排序
            hotfixMgrList.Sort((a, b) =>
            {
                var aAttr = a.GetType().GetCustomAttribute<ManagerOrder>();
                var bAttr = a.GetType().GetCustomAttribute<ManagerOrder>();
                var aOrder = aAttr == null ? 0 : aAttr.Order;
                var bOrder = bAttr == null ? 0 : bAttr.Order;
                //对比
                return aOrder.CompareTo(bOrder);
            });

            RegisterHotfixType(hotfixTypes);

            BDebug.Log("管理器CheckType完成", Color.green);
            BDebug.Log("管理器数量" + hotfixMgrList.Count, Color.red);
            return hotfixMgrList.ToArray();
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        /// <param name="types"></param>
        static private void RegisterHotfixType(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (type != null && type.IsClass)
                {
                    var mgrAttributes = type.GetAttributeInILRuntimes<ManagerAttribute>();
                    if (mgrAttributes != null)
                    {
                        //注册类型
                        foreach (var mgr in hotfixMgrList)
                        {
                            var ret = mgr.RegisterTypes(type, mgrAttributes);
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
        /// 注册主工程类型
        /// </summary>
        /// <param name="types"></param>
        static public void RegisterMainProjectType(IEnumerable<string> replaceMgrNames, IEnumerable<Type> types)
        {
            if (replaceMgrNames.Count() > 0)
            {
                var replacedMgrList = hotfixMgrList.Where((mgr) => replaceMgrNames.Contains(mgr.GetType().Name));
                BDebug.Log($"热更mgr接管:{JsonMapper.ToJson(replacedMgrList.Select((m)=>m.GetType().Name).ToArray(),true)}", Color.magenta);
                foreach (var type in types)
                {
                    if (type != null && type.IsClass)
                    {
                        var mgrAttributes = type.GetAttributeInILRuntimes<ManagerAttribute>();
                        if (mgrAttributes != null)
                        {
                            //注册类型
                            foreach (var mgr in replacedMgrList)
                            {
                                var ret = mgr.RegisterTypes(type, mgrAttributes);
                                if (ret)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                
                //添加出去
                ManagerInstHelper.MgrList.AddRange(replacedMgrList);
            }
        }

        /// <summary>
        /// 开始
        /// </summary>
        static public void Start()
        {
            //管理器初始化
            foreach (var m in hotfixMgrList)
            {
                BDebug.Log("[hotfix ]init" + m.GetType().FullName, Color.yellow);
                m.Init();
            }

            BDebug.Log("[hotfix]热更管理器启动", Color.green);
            foreach (var hotfixMgr in hotfixMgrList)
            {
                hotfixMgr.Start();
            }
        }
    }
}