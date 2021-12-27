using System.Collections.Generic;
using System.Reflection;
using ILRuntime.CLR.Method;
using ILRuntime.CLR.Utils;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using LitJson;
using SQLite4Unity3d;
using UnityEngine;

namespace BDFramework.Sql
{
    /// <summary>
    /// 这里主要是为了和主工程隔离
    /// hotfix专用的Sql Helper
    /// </summary>
    static public class SqliteHelper
    {
        //
        static private SQLiteService dbservice;

        //现在是热更层不负责加载,只负责使用
        static public SQLiteService DB
        {
            get
            {
                if (dbservice == null || dbservice.IsClose)
                {
                    dbservice = new SQLiteService(SqliteLoder.Connection);
                    if (dbservice == null)
                    {
                        BDebug.LogError("Sql加载失败，请检查!");
                    }
                }

                return dbservice;
            }
        }


        #region ILRuntime

        /// <summary>
        /// 注册SqliteHelper的ILR重定向
        /// </summary>
        /// <param name="appdomain"></param>
        public unsafe static void RegisterILRuntimeCLRRedirection(ILRuntime.Runtime.Enviorment.AppDomain appdomain)
        {
            foreach (var mi in typeof(TableQueryILRuntime).GetMethods())
            {
                if (mi.Name == "FromAll" && mi.IsGenericMethodDefinition)
                {
                    var param = mi.GetParameters();
                    if (param[0].ParameterType == typeof(string))
                    {
                        appdomain.RegisterCLRMethodRedirection(mi, ReDirFromAll);
                    }
                }
            }
        }

        /// <summary>
        /// 查询的重定向
        /// </summary>
        /// <param name="intp"></param>
        /// <param name="esp"></param>
        /// <param name="mStack"></param>
        /// <param name="method"></param>
        /// <param name="isNewObj"></param>
        /// <returns></returns>
        public unsafe static StackObject* ReDirFromAll(ILIntepreter intp, StackObject* esp, IList<object> mStack, CLRMethod method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(esp, 1);
            ptr_of_this_method = ILIntepreter.Minus(esp, 1);
            //
            System.String selection = (System.String) typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, mStack));
            intp.Free(ptr_of_this_method);
            var type = method.GenericArguments[0].ReflectionType;
            //调用
            var result_of_this_method = DB.GetTableRuntime().FormAll(type, selection);
            //转成ilrTypeInstance
            var retList = new List<ILTypeInstance>(result_of_this_method.Count);
            for (int i = 0; i < result_of_this_method.Count; i++)
            {
                var hotfixObj = result_of_this_method[i] as ILTypeInstance;
                retList.Add(hotfixObj);
            }

            return ILIntepreter.PushObject(__ret, mStack, retList);
        }

        #endregion
    }
}