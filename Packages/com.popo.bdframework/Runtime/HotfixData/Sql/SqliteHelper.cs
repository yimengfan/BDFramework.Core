using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            /// 旧运行时查询包装器实例。
            /// Legacy runtime query wrapper instance.
            /// </summary>
            private readonly TableQueryForILRuntime _ilRuntimeTable;


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

            /// <summary>
            /// 获取运行时查询构建器（兼容旧 TableQueryForILRuntime API）。
            /// 返回独立的兼容包装层，保留旧链式查询接口并映射到当前 sqlite 查询实现。
            /// Get the runtime query builder compatible with the legacy TableQueryForILRuntime API.
            /// Returns a standalone compatibility wrapper that preserves the legacy fluent API
            /// and maps it onto the current sqlite query implementation.
            /// </summary>
            public TableQueryForILRuntime ILRuntimeTable
            {
                get { return _ilRuntimeTable; }
            }

            /// <summary>
            /// 获取运行时查询构建器（兼容旧 TableQueryForILRuntime API）。
            /// 返回缓存的兼容包装器实例，保持旧属性/方法两种访问方式都可用。
            /// Get the runtime query builder compatible with the legacy TableQueryForILRuntime API.
            /// Returns the cached compatibility wrapper so both legacy property and method access stay valid.
            /// </summary>
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
                    if (SqliteLoder.Connection != null && SqliteLoder.Connection.IsOpen)
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
                // 防御性空检查：SQLite 连接可能在 Close 后被移除，GetSqliteConnect 返回 null
                // Defensive null check: SQLite connection may have been removed after Close, GetSqliteConnect returns null
                if (con != null && con.IsOpen)
                {
                    db = new SQLiteService(con);
                    DBServiceMap[dbName] = db;
                }
                else
                {
                    // 连接不可用时移除缓存以避免后续调用直接返回已关闭的旧实例
                    // When connection is unavailable, remove cached entry to avoid returning stale closed instance
                    DBServiceMap.Remove(dbName);
                    db = null;
                }
            }

            return db;
        }

        /// <summary>
        /// 移除缓存的 DB 服务实例。
        /// 由 SqliteLoder.Close 调用以确保连接关闭后 DBServiceMap 中不留已过期的缓存条目。
        /// Remove a cached DB service instance.
        /// Called by SqliteLoder.Close to ensure DBServiceMap doesn't retain stale entries after connection is closed.
        /// </summary>
        /// <param name="dbName">DB 名称 / DB name</param>
        static public void RemoveDBService(string dbName)
        {
            if (!string.IsNullOrEmpty(dbName))
            {
                DBServiceMap.Remove(dbName);
            }
        }
    }
}
