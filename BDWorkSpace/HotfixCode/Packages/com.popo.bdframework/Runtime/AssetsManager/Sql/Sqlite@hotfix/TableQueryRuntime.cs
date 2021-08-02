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
using Cysharp.Text;
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


        private string @where = null;
        private string @sql   = null;
        private string @limit = null;

        public TableQueryILRuntime(SQLiteConnection connection)
        {
            this.Connection = connection;
        }


        #region 数据库直接操作

        private SQLiteCommand GenerateCommand(string @select, string tablename)
        {
            string cmdText = "";

            //select where语句

            if (@sql == null)
            {
                if (!string.IsNullOrEmpty(@where))
                {
                    cmdText = ZString.Format("select {0} from {1} where {2}", @select, tablename, @where);

                    //limit语句
                    if (!string.IsNullOrEmpty(this.limit))
                    {
                        cmdText = ZString.Concat(cmdText, " Limit ", limit);
                    }
                }
                else
                {
                    cmdText = ZString.Format("select {0} from {1}", @select, tablename);
                }
            }
            else
            {
                //直接执行sql
                cmdText = @sql;
            }


#if UNITY_EDITOR
            BDebug.Log("sql:" + cmdText);
#endif

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



        #region Where、or、And 、Limit

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
        /// Limit 语句
        /// </summary>
        /// <param name="limitValue"></param>
        public TableQueryILRuntime Limit(int limitValue)
        {
            this.limit = limitValue.ToString();

            return this;
        }

        #endregion


        #region Select语句

        /// <summary>
        /// forilruntime
        /// </summary>
        /// <returns></returns>
        public T From<T>(string selection = "*") where T : class, new()
        {
            var rets = this.Limit(1).FromAll<T>(selection);

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
                var t = (T)o;
                results.Add(t);
            }

            return results;
        }

        #endregion
    }
}