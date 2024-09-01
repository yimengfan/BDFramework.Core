using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using ILRuntime.CLR.Method;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.CLR.Utils;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using SQLite4Unity3d;
using UnityEngine;

//这里为了方便切换Sqlite-net版本 将三个类放在一起
namespace BDFramework.Sql
{
    /// <summary>
    /// Sqlite 加载器
    /// </summary>
    static public class SqliteLoder
    {
        static readonly string Tag = "SQLite";

        public static string password;

        /// <summary>
        /// Password
        /// </summary>
        public static string Password
        {
            get
            {
                if (!string.IsNullOrEmpty(password))
                {
                    return password;
                }
                else //配置未初始化的情况
                {
                    var conf = GameConfigManager.Inst.GetConfig<GameCipherConfigProcessor.Config>();
                    return conf.SqlitePassword;
                }
            }
            set { password = value; }
        }

        /// <summary>
        /// 本地DB Path
        /// </summary>
        public readonly static string LOCAL_DB_PATH = "local.db";

        /// <summary>
        /// ServerDB Path
        /// </summary>
        public readonly static string SERVER_DB_PATH = "server.db";

        /// <summary>
        /// sql驱动对象
        /// </summary>
        static public SQLiteConnection Connection { get; set; }

        /// <summary>
        /// DB连接库
        /// </summary>
        private static Dictionary<string, SQLiteConnection> SqLiteConnectionMap =
            new Dictionary<string, SQLiteConnection>();

        /// <summary>
        /// runtime下加载，只读
        /// </summary>
        /// <param name="str"></param>
        static public void Init(AssetLoadPathType assetLoadPathType)
        {
            BDebug.EnableLog(Tag);
            Connection?.Dispose();
            var path = GameBaseConfigProcessor.GetLoadPath(assetLoadPathType);
            //用当前平台目录进行加载
            path = GetLocalDBPath(path, BApplication.RuntimePlatform);
            Connection = LoadDBReadOnly(path);
        }


        /// <summary>
        /// 加载db 只读
        /// </summary>
        static public SQLiteConnection LoadDBReadOnly(string path)
        {
            if (File.Exists(path))
            {
                BDebug.Log(Tag, $"加载路径:{path} psw:{Password}", Color.green);
                SQLiteConnectionString cs =
                    new SQLiteConnectionString(path, SQLiteOpenFlags.ReadOnly, true, key: Password);
                var con = new SQLiteConnection(cs);
                SqLiteConnectionMap[Path.GetFileNameWithoutExtension(path)] = con;
                return con;
            }
            else
            {
                Debug.LogError("DB不存在:" + path);
                return null;
            }
        }

        /// <summary>
        /// 加载db ReadWriteCreate
        /// </summary>
        static public SQLiteConnection LoadDBReadWriteCreate(string path)
        {
            BDebug.Log($" DB Path:{path}  <color=yellow>password:{Password}</color>");
            SQLiteConnectionString cs = new SQLiteConnectionString(path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true, key: Password);
            var con = new SQLiteConnection(cs);
            SqLiteConnectionMap[Path.GetFileNameWithoutExtension(path)] = con;
            return con;
        }


        /// <summary>
        /// sqliteConnect
        /// </summary>
        /// <param name="dbname"></param>
        /// <returns></returns>
        static public SQLiteConnection GetSqliteConnect(string dbname)
        {
            SqLiteConnectionMap.TryGetValue(dbname, out var con);
            return con;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        static public void Close(string dbName = "")
        {
            if (string.IsNullOrEmpty(dbName))
            {
                Connection?.Dispose();
                Connection = null;
            }
            else
            {
                var ret = SqLiteConnectionMap.TryGetValue(dbName, out var con);
                if (ret)
                {
                    con.Dispose();
                    SqLiteConnectionMap.Remove(dbName);
                }
            }
        }


        /// <summary>
        /// 获取DB路径
        /// </summary>
        static public string GetLocalDBPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BApplication.GetPlatformPath(platform), LOCAL_DB_PATH);
        }

        /// <summary>
        /// 获取DB路径
        /// </summary>
        static public string GetServerDBPath(string root, RuntimePlatform platform)
        {
            return IPath.Combine(root, BApplication.GetPlatformPath(platform), SERVER_DB_PATH);
        }

        #region Editor下加载

        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public string LoadLocalDBOnEditor(string root, RuntimePlatform platform)
        {
            //用当前平台目录进行加载
            var path = GetLocalDBPath(root, platform);
            LoadSQLOnEditor(path);

            return path;
        }


        /// <summary>
        /// 编辑器下加载DB，可读写|创建
        /// </summary>
        /// <param name="str"></param>
        static public void LoadServerDBOnEditor(string root, RuntimePlatform platform)
        {
            //用当前平台目录进行加载
            var path = GetServerDBPath(root, platform);
            LoadSQLOnEditor(path);
        }

        /// <summary>
        /// 加载Sql
        /// </summary>
        /// <param name="sqlPath"></param>
        static public void LoadSQLOnEditor(string sqlPath)
        {
            //
            Connection?.Dispose();
            //
            var dir = Path.GetDirectoryName(sqlPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            //编辑器下打开
            if (Application.isEditor)
            {
                //editor下 不在执行的时候，直接创建
                Connection = LoadDBReadWriteCreate(sqlPath);
                BDebug.Log("DB加载路径:" + sqlPath, Color.red);
            }
        }

        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <param name="root"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        static public string DeleteDBFile(string root, RuntimePlatform platform)
        {
            //用当前平台目录进行加载
            var path = GetLocalDBPath(root, platform);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return path;
        }

        #endregion
    }

    /// <summary>
    /// Sqlite辅助类
    /// </summary>
    static public class SqliteHelper
    {
        /// <summary>
        /// sqlite服务
        /// </summary>
        public class SQLiteService
        {
            //db connect
            public SQLiteConnection Connection { get; private set; }


            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="con"></param>
            public SQLiteService(SQLiteConnection con)
            {
                this.Connection = con;
                this._ilRuntimeTable = new TableQueryForILRuntime(this.Connection);
            }

            /// <summary>
            /// 是否关闭
            /// </summary>
            public bool IsClose
            {
                get { return Connection == null || !Connection.IsOpen; }
            }

            /// <summary>
            /// DB路径
            /// </summary>
            public string DBPath
            {
                get { return this.Connection.DatabasePath; }
            }

            #region 常见的表格操作

            /// <summary>
            /// 创建db
            /// </summary>
            /// <typeparam name="T"></typeparam>
            public void CreateTable<T>()
            {
                CreateTable(typeof(T));
            }

            /// <summary>
            /// 创建db
            /// </summary>
            /// <param name="t"></param>
            public void CreateTable(Type t)
            {
                Connection.DropTable(t);
                Connection.CreateTable(t);
            }

            /// <summary>
            /// 插入数据
            /// </summary>
            /// <param name="objects"></param>
            public void InsertTable(System.Collections.IEnumerable objects)
            {
                Connection.InsertAll(objects);
            }

            /// <summary>
            /// 插入数据
            /// </summary>
            /// <param name="objects"></param>
            public void Insert(object @object)
            {
                Connection.Insert(@object);
            }

            /// <summary>
            /// 插入所有
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="objTypes"></param>
            public void InsertAll<T>(List<T> obj)
            {
                Connection.Insert(@obj, typeof(T));
            }

            /// <summary>
            /// 获取表
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public TableQuery<T> GetTable<T>() where T : new()
            {
                return new TableQuery<T>(Connection);
            }

            #endregion

            #region for ILRuntime

            /// <summary>
            /// ILRuntime的table
            /// </summary>
            private TableQueryForILRuntime _ilRuntimeTable;

            /// <summary>
            /// 获取TableRuntime
            /// </summary>
            public TableQueryForILRuntime ILRuntimeTable
            {
                get { return _ilRuntimeTable; }
            }

            /// <summary>
            /// 获取TableRuntime
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public TableQueryForILRuntime GetTableRuntime()
            {
                return _ilRuntimeTable;
            }

            #endregion
        }

        /// <summary>
        /// db服务
        /// </summary>
        static private SQLiteService dbservice;

        /// <summary>
        /// 获取主DB
        /// </summary>
        static public SQLiteService DB
        {
            get
            {
                if (dbservice == null || dbservice.IsClose) //防止持有未关闭的db connect
                {
                    if (SqliteLoder.Connection.IsOpen)
                    {
                        dbservice = new SQLiteService(SqliteLoder.Connection);
                    }
                }

                return dbservice;
            }
        }

        /// <summary>
        /// db map
        /// </summary>
        private static Dictionary<string, SQLiteService> DBServiceMap = new Dictionary<string, SQLiteService>();

        /// <summary>
        /// 获取一个DB
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        static public SQLiteService GetDB(string dbName)
        {
            SQLiteService db = null;
            if (!DBServiceMap.TryGetValue(dbName, out db) || db.IsClose) //防止持有未关闭的db connect
            {
                var con = SqliteLoder.GetSqliteConnect(dbName);
                if (con.IsOpen)
                {
                    db = new SQLiteService(con);
                    DBServiceMap[dbName] = db;
                }
            }

            return db;
        }


        #region ILRuntime 重定向

        /// <summary>
        /// 注册SqliteHelper的ILR重定向
        /// </summary>
        /// <param name="appdomain"></param>
        public unsafe static void RegisterCLRRedirection(ILRuntime.Runtime.Enviorment.AppDomain appdomain)
        {
            //tableRuntime 绑定
            foreach (var mi in typeof(TableQueryForILRuntime).GetMethods())
            {
                if (mi.Name == "FromAll" && mi.IsGenericMethodDefinition && mi.GetParameters().Length == 1)
                {
                    appdomain.RegisterCLRMethodRedirection(mi, RedirFromAll);
                }
                else if (mi.Name == "From" && mi.IsGenericMethodDefinition && mi.GetParameters().Length == 1)
                {
                    appdomain.RegisterCLRMethodRedirection(mi, RedirFrom);
                }
            }


            //service绑定
            foreach (var mi in typeof(SQLiteService).GetMethods())
            {
                if (mi.Name == "CreateTable" && mi.GetParameters().Length == 0)
                {
                    appdomain.RegisterCLRMethodRedirection(mi, RedirCreateTable);
                    break;
                }
            }
        }


        /// <summary>
        /// FromAll的重定向
        /// </summary>
        /// <param name="intp"></param>
        /// <param name="esp"></param>
        /// <param name="mStack"></param>
        /// <param name="method"></param>
        /// <param name="isNewObj"></param>
        /// <returns></returns>
        unsafe static StackObject* RedirFromAll(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack,
            CLRMethod method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @selection = (System.String) typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), 0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            SQLite4Unity3d.TableQueryForILRuntime instance_of_this_method = (SQLite4Unity3d.TableQueryForILRuntime) typeof(SQLite4Unity3d.TableQueryForILRuntime).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), 0);
            __intp.Free(ptr_of_this_method);

            //调用
            var generic = method.GenericArguments[0];
            var result_of_this_method = instance_of_this_method.FromAll(generic.ReflectionType, selection);

            if (generic is CLRType)
            {
                // 创建clrTypeInstance
                var clrType = generic.TypeForCLR;
                var genericType = typeof(List<>).MakeGenericType(clrType);
                var retList = (IList) Activator.CreateInstance(genericType);

                for (int i = 0; i < result_of_this_method.Count; i++)
                {
                    var obj = result_of_this_method[i];
                    retList.Add(obj);
                }

                return ILIntepreter.PushObject(__ret, __mStack, retList);
            }
            else
            {
                // 转成ilrTypeInstance
                var retList = new List<ILTypeInstance>(result_of_this_method.Count);
                for (int i = 0; i < result_of_this_method.Count; i++)
                {
                    var hotfixObj = result_of_this_method[i] as ILTypeInstance;
                    retList.Add(hotfixObj);
                }

                return ILIntepreter.PushObject(__ret, __mStack, retList);
            }
        }


        /// <summary>
        /// From重定向
        /// </summary>
        /// <param name="intp"></param>
        /// <param name="esp"></param>
        /// <param name="mStack"></param>
        /// <param name="method"></param>
        /// <param name="isNewObj"></param>
        /// <returns></returns>
        unsafe static StackObject* RedirFrom(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack,
            CLRMethod method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @selection = (System.String) typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), 0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            SQLite4Unity3d.TableQueryForILRuntime instance_of_this_method = (SQLite4Unity3d.TableQueryForILRuntime) typeof(SQLite4Unity3d.TableQueryForILRuntime).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), 0);
            __intp.Free(ptr_of_this_method);

            //调用
            var generic = method.GenericArguments[0];
            var result_of_this_method = instance_of_this_method.From(generic.ReflectionType, selection);

            // if (generic is CLRType)
            // {
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
            // }
            // else
            // {
            //     // 转成ilrTypeInstance
            //
            //     var ilrInstance = result_of_this_method as ILTypeInstance;
            //     return ILIntepreter.PushObject(__ret, mStack, ilrInstance);
            // }
        }


        unsafe static StackObject* RedirCreateTable(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack,
            CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            SQLiteService instance_of_this_method = (SQLiteService) typeof(SQLiteService).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (ILRuntime.CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);

            var generic = __method.GenericArguments[0];
            instance_of_this_method.CreateTable(generic.ReflectionType);

            return __ret;
        }

        #endregion
    }
}
