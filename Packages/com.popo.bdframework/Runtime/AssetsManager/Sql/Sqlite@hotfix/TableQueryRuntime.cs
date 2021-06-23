#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using BDFramework;
using LitJson;
using Debug = UnityEngine.Debug;
#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;

#endif
namespace SQLite4Unity3d
{
    public class TableQueryILRuntime : BaseTableQuery
    {
        public SQLiteConnection Connection { get; private set; }


        private string @where = "";
        private string @sql = null;

        public TableQueryILRuntime(SQLiteConnection connection)
        {
            this.Connection = connection;
        }


        #region 数据库直接操作

        private SQLiteCommand GenerateCommand(string selection, string tablename)
        {
            //0表名
            string cmdText = "";

            //select where语句
            if (!string.IsNullOrEmpty(@where))
            {
                cmdText = "select " + selection + " from {0} {1}";
                @sql = "where " + @where;
                cmdText = string.Format(cmdText, tablename, @sql);
            }
            else if (@sql == null)
            {
                cmdText = "select " + selection + " from {0}";
                cmdText = string.Format(cmdText, tablename, @sql);
            }
            else
            {
                //直接执行sql
                cmdText = @sql;
            }


            // BDebug.Log("sql:" + cmdText);
            return Connection.CreateCommand(cmdText);
        }

        #endregion


        /// <summary>
        /// 直接执行sql
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public TableQueryILRuntime Exec(string sql)
        {
            this.@sql = sql;
            return this;
        }


        #region Where数据库操作  by BDFramework

        /// <summary>
        /// Where语句
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime Where(string where, object value)
        {
            if (value is string)
            {
                value = string.Format("'{0}'", value);
            }

            this.@where += string.Format((" " + where), value);
            return this;
        }

        /// <summary>
        /// Where语句
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime Where(string where)
        {
            this.@where += string.Format((" " + where), where);
            return this;
        }

        /// <summary>
        /// and语句
        /// </summary>
        /// <param name="where"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public TableQueryILRuntime And
        {
            get
            {
                this.@where += " and";
                return this;
            }
           
        }

        /// <summary>
        /// Or 语句
        /// </summary>
        /// <returns></returns>
        public TableQueryILRuntime Or
        {
            get
            {
                this.@where += " or";
                return this;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime WhereOr(string field, string operation = "", params object[] objs)
        {
            string sql = "";
            for (int i = 0; i < objs.Length; i++)
            {
                var value = objs[i].ToString();
                if (sql == "")
                {
                    sql += string.Format(" {0} {1} {2}", field, operation, value);
                }
                else
                {
                    sql += string.Format(" or {0} {1} {2}", field, operation, value);
                }
            }

            this.@where = sql;
            return this;
        }

        /// <summary>
        /// Where and
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime WhereAnd(string field, string operation = "", params object[] objs)
        {
            string sql = "";
            for (int i = 0; i < objs.Length; i++)
            {
                var value = objs[i].ToString();
                if (sql == "")
                {
                    sql += string.Format(" {0} {1} {2}", field, operation, value);
                }
                else
                {
                    sql += string.Format(" and {0} {1} {2}", field, operation, value);
                }
            }

            this.@where = sql;
            return this;
        }

        /// <summary>
        /// forilruntime
        /// </summary>
        /// <returns></returns>
        public T From<T>(string selection = "*") where T : class, new()
        {
            var rets = FromAll<T>(selection);
            if (rets.Count > 0)
            {
                return rets[0];
            }

            return null;
        }

        public List<T> FromAll<T>(string selection = "*") where T : new()
        {
            var type    = typeof(T);
            var results = new List<T>();
            //查询所有数据
            var cmd  = GenerateCommand(selection, type.Name);
            var list = cmd.ExecuteQuery(typeof(T));
            foreach (var o in list)
            {
                var t = (T) o;
                results.Add(t);
            }

            return results;
        }
        
        
        
        #endregion
    }
}