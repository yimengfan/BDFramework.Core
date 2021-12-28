#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using BDFramework;
using Cysharp.Text;
using ILRuntime.CLR.Method;
using ILRuntime.CLR.Utils;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
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
        private string @sql = null;
        private string @limit = null;

        public TableQueryILRuntime(SQLiteConnection connection)
        {
            this.Connection = connection;
        }


        #region 数据库直接操作

        private SQLiteCommand GenerateCommand(string @select, string tablename)
        {
            string sqlCmdText = "";

            //select where语句

            if (@sql == null)
            {
                //基本语句
                sqlCmdText = ZString.Format("select {0} from {1}", @select, tablename);
                //Where语句
                if (!string.IsNullOrEmpty(@where))
                {
                    sqlCmdText = ZString.Concat(sqlCmdText, " where", @where);
                }

                //limit语句
                if (!string.IsNullOrEmpty(this.limit))
                {
                    sqlCmdText = ZString.Concat(sqlCmdText, " Limit ", limit);
                }
            }
            else
            {
                //直接执行sql
                sqlCmdText = @sql;
            }


#if UNITY_EDITOR
            //BDebug.Log("sql:" + cmdText);
#endif

            return Connection.CreateCommand(sqlCmdText);
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
                value = ZString.Format("'{0}'", value);
            }

            this.@where = ZString.Concat(this.@where, " ", ZString.Format(where, value));
            return this;
        }

        /// <summary>
        /// Where语句
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime Where(string where)
        {
            this.@where = ZString.Concat(this.@where, " ", where); // string.Format((" " + where), where);
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
                this.@where = ZString.Concat(this.@where, " and");
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
                this.@where = ZString.Concat(this.@where, " or");
                return this;
            }
        }

        /// <summary>
        /// In语句查询
        /// </summary>
        public TableQueryILRuntime WhereIn<T>(string field, IEnumerable<T> values)
        {
            var sqlIn = string.Join(",", values);
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlIn);

            return this;
        }

        /// <summary>
        /// In语句查询
        /// </summary>
        public TableQueryILRuntime WhereIn(string field, params object[] objs)
        {
            var sqlIn = string.Join(",", objs);
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlIn);

            return this;
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
                    sql = ZString.Format(" {0} {1} {2}", field, operation, value);
                }
                else
                {
                    sql += ZString.Format(" or {0} {1} {2}", field, operation, value);
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
                    sql += ZString.Format(" {0} {1} {2}", field, operation, value);
                }
                else
                {
                    sql += ZString.Format(" and {0} {1} {2}", field, operation, value);
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


        #region 排序

        /// <summary>
        /// 降序排序
        /// </summary>
        public TableQueryILRuntime OrderByDesc(string field)
        {
            var query = ZString.Format(" Order By {0} Desc", field);
            this.@where = ZString.Concat(this.@where, query);
            return this;
        }

        /// <summary>
        /// 升序排序
        /// </summary>
        public TableQueryILRuntime OrderBy(string field)
        {
            var query = ZString.Format(" Order By {0}", field);
            this.@where = ZString.Concat(this.@where, query);
            return this;
        }
        #endregion


        #region Select语句

        /// <summary>
        /// forilruntime
        /// </summary>
        /// <returns></returns>
        public T From<T>(string selection = "*")
        {
            var rets = this.Limit(1).FromAll<T>(selection);

            if (rets.Count > 0)
            {
                return rets[0];
            }

            return default(T);
        }


        /// <summary>
        /// 查询所有的数据
        /// </summary>
        /// <param name="selection"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> FromAll<T>(string selection = "*")
        {
            //查询
            var list = this.FormAll(typeof(T), selection);

            var retList = new List<T>(list.Count);
            //映射并返回T
            for (int i = 0; i < list.Count; i++)
            {
                retList[i] = (T) list[i];
            }

            return retList;
        }

        /// <summary>
        /// 非泛型方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        public List<object> FormAll(Type type, string selection = "*")
        {
            var cmd = GenerateCommand(selection, type.Name);
            var list = cmd.ExecuteQuery(type);
            BDebug.Log(cmd.CommandText);
            return list;
        }

        #endregion
    }
}