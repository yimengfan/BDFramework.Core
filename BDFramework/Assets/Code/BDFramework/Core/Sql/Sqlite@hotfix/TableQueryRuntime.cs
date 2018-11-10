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
        private string like   = "";
        private string limit  = "";
       
        public TableQueryILRuntime( SQLiteConnection connection)
        {
            this.Connection = connection;
        }



        #region 数据库直接操作

        private SQLiteCommand GenerateCommand(string selection , string tablename)
        {
            //0表名
            string cmdText = "select "+selection+" from {0} {1}";
            if (string.IsNullOrEmpty(@where) == false)
            {
                @where = "where " + @where;
            }
            
            cmdText= string.Format(cmdText,tablename, @where);
            
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
        public TableQueryILRuntime Where(string where ,params object[] formats)
        {
            if (formats.Length > 0)
            {
                this.@where = string.Format(where, formats);
            }
            else
            {
                this.@where = where;
            }
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime WhereOr(string field,string operation ,List<object> objs)
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

            this.@where = sql;
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryILRuntime WhereAnd(string field,string operation,IList objs)
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

            this.@where = sql;
            return  this;
        }
        /// <summary>
        /// forilruntime
        /// </summary>
        /// <returns></returns>
        public List<T> ToSearch<T>(string selection = "*") where T : new()
        {  
            var DataCache = new List<T>();
            //查询所有数据
            var cmd = GenerateCommand(selection, typeof(T).Name.ToLower());
            var list = cmd.ExecuteQuery(typeof(T));
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