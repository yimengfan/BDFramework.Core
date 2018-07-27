#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif
using System;
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
   
    public class TableQueryILRuntime<T> : BaseTableQuery
    {
        public SQLiteConnection Connection { get; private set; }

        public TableMapping Table { get; private set; }

        public string _Where = "";
        public string _Like = "";
        public string _Limit = "";
       

          
        TableQueryILRuntime(SQLiteConnection conn, TableMapping table)
        {
            Connection = conn;
            Table = table;
        }


        public TableQueryILRuntime(SQLiteConnection conn)
        {
            Connection = conn;
            var t = typeof(T);
            Table = Connection.GetMapping(t);
        }

        public TableQueryILRuntime<U> Clone<U>()
        {
            var q = new TableQueryILRuntime<U>(Connection, Table);
            q._Where = this._Where;
            q._Like  = this._Like;
            q._Limit = this._Limit;
            
            return q;
        }


        #region 数据库直接操作

        private SQLiteCommand GenerateCommand(string selectionList)
        {
            //0表名
            string cmdText = "select * from {0} {1}";
            if (string.IsNullOrEmpty(_Where) == false)
            {
                _Where = "where " + _Where;
            }
            
            cmdText= string.Format(cmdText, Table.TableName, _Where);
            
            BDebug.Log("sql:" + cmdText);
            return Connection.CreateCommand(cmdText);
        }

        #endregion


        #region 数据库操作  by BDFramework
        /// <summary>
        /// 基本语法
        /// 1.ID == 1
        /// 2.ID > 1 And ID < 5
        /// 3.ID > 1 Or  ID < -1
        /// 4.Id BETWEEN 25 AND 27;
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime<T> Where(string where ,params  object[] formats)
        {
            if (formats.Length > 0)
            {
                this._Where = string.Format(where, formats);
            }
            else
            {
                this._Where = where;
            }
            return  Clone<T>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime<T> WhereOr<V>(string field,string operation ,List<V> objs)
        {
            string sql = "";
            for (int i = 0; i < objs.Count; i++)
            {
                var value = objs[i].ToString();
                if (i == 0)
                {
                    sql = string.Format("{0} {1} {2}", field,operation,value);
                }
                else
                {
                    sql += string.Format(" or {0} {1} {2}", field,operation,value);
                } 
            }

            this._Where = sql;
            return  Clone<T>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime<T> WhereAnd<V>(string field,string operation ,List<V> objs)
        {
            string sql = "";
            for (int i = 0; i < objs.Count; i++)
            {
                var value = objs[i].ToString();
                if (i == 0)
                {
                    sql = string.Format("{0} {1} {2}", field,operation,value);
                }
                else
                {
                    sql += string.Format(" and {0} {1} {2}", field,operation,value);
                } 
            }      
            return  Clone<T>();
        }
        /// <summary>
        /// forilruntime
        /// </summary>
        /// <returns></returns>
        public List<T> ToSearch(string sql = "*")
        {  
            var DataCache = new List<T>();
            //查询所有数据
            var list = GenerateCommand(sql).ExecuteQuery(typeof(T));
            foreach (var o in list)
            {
                var _t = (T) o;
                DataCache.Add(_t);
            }      
            return DataCache;
        }

        #endregion
    }
}