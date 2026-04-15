using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using SQLite4Unity3d;
using UnityEngine;

//这里为了方便切换Sqlite-net版本 将三个类放在一起
namespace BDFramework.Sql
{

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
        private static Dictionary<string, SQLiteService> DBServiceMap { get; set; } = new Dictionary<string, SQLiteService>();

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
    }
}
