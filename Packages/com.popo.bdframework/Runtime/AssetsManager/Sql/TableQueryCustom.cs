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
using UnityEngine;
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
    /// <summary>
    /// 自定义版本的 TableQuery
    /// </summary>
    public class TableQueryCustom : BaseTableQuery
    {
        public SQLiteConnection Connection { get; private set; }


        #region 语句缓存

        private string @where = "";
        private string @sql = "";
        private string @limit = "";

        #endregion

        /// <summary>
        /// sql缓存的触发次数
        /// 不能为0
        /// </summary>
        private int TRIGGER_CHACHE_NUM = 3;
        /// <summary>
        /// sql缓存触发消耗时间
        /// 不能为0
        /// </summary>
        private float TRIGGER_CHACHE_TIMER = 0.05f;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connection">sql连接器</param>
        /// <param name="triggerCacheNum">触发缓存次数</param>
        public TableQueryCustom(SQLiteConnection connection)
        {
            this.Connection = connection;
        }

        /// <summary>
        /// 设置sql 缓存触发参数
        /// </summary>
        /// <param name="triggerCacheNum"></param>
        /// <param name="triggerChacheTimer"></param>
        public void EnableSqlCahce(int triggerCacheNum = 5, float triggerChacheTimer = 0.05f)
        {
            this.TRIGGER_CHACHE_NUM = triggerCacheNum;
            this.TRIGGER_CHACHE_TIMER = triggerChacheTimer;
        }

        #region 生成sql cmd

        private string GenerateCommand(string @select, string tablename)
        {
            string sqlCmdText = "";

            //select where语句

            if (string.IsNullOrEmpty(@sql))
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
            Debug.Log("sql:" + sqlCmdText);
#endif

            return sqlCmdText;
        }

        #endregion


        /// <summary>
        /// 直接执行sql
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public TableQueryCustom Exec(string sql)
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
        public TableQueryCustom Where(string where, object value)
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
        public TableQueryCustom Where(string where)
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
        public TableQueryCustom And
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
        public TableQueryCustom Or
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
        public TableQueryCustom WhereIn<T>(string field, IEnumerable<T> values)
        {
            var sqlIn = string.Join(",", values);
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlIn);

            return this;
        }

        /// <summary>
        /// In语句查询
        /// </summary>
        public TableQueryCustom WhereIn(string field, params object[] objs)
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
        public TableQueryCustom WhereOr(string field, string operation = "", params object[] objs)
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
        public TableQueryCustom WhereAnd(string field, string operation = "", params object[] objs)
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

        #endregion

        #region Limit语句

        /// <summary>
        /// Limit 语句
        /// </summary>
        /// <param name="limitValue"></param>
        public TableQueryCustom Limit(int limitValue)
        {
            this.limit = limitValue.ToString();

            return this;
        }

        #endregion

        #region 排序

        /// <summary>
        /// 降序排序
        /// </summary>
        public TableQueryCustom OrderByDesc(string field)
        {
            var query = ZString.Format(" Order By {0} Desc", field);
            this.@where = ZString.Concat(this.@where, query);
            return this;
        }

        /// <summary>
        /// 升序排序
        /// </summary>
        public TableQueryCustom OrderBy(string field)
        {
            var query = ZString.Format(" Order By {0}", field);
            this.@where = ZString.Concat(this.@where, query);
            return this;
        }

        #endregion

        #region Select、From语句

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
                if (list[i] is T tObj)
                {
                    retList.Add(tObj);
                }
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
            var sqlCmdText = GenerateCommand(selection, type.Name);
            List<object> retlist = null;
            
            if (this.TRIGGER_CHACHE_NUM > 0 || this.TRIGGER_CHACHE_TIMER > 0)
            {
                //判断是否在缓存中
                var ret = sqlResultCacheMap.TryGetValue(sqlCmdText, out retlist);
                if (!ret)
                {
                    var st = Time.realtimeSinceStartup;
                    //查询
                    {
                        var cmd = this.Connection.CreateCommand(sqlCmdText);
                        retlist = cmd.ExecuteQuery(type);
                    }
                    var intelval = Time.realtimeSinceStartup - st;
                    //缓存判断
                    var counter = GetSqlExecCount(sqlCmdText);
                    if (counter >= this.TRIGGER_CHACHE_NUM || intelval >= this.TRIGGER_CHACHE_TIMER)
                    {
                        this.AddSqlCache(sqlCmdText, retlist);
                    }
                    else
                    {
                        this.AddSqlExecCounter(sqlCmdText, counter);
                    }
                }
            }
            else
            {
                //查询
                var cmd = this.Connection.CreateCommand(sqlCmdText);
                retlist = cmd.ExecuteQuery(type);
            }
            
            //重置状态
            this.Reset();
            return retlist;
        }

        #endregion

        #region 缓存

        /// <summary>
        /// 缓存列表
        /// </summary>
        public Dictionary<string, List<object>> sqlResultCacheMap = new Dictionary<string, List<object>>();

        /// <summary>
        /// 添加sql缓存
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ret"></param>
        public void AddSqlCache(string cmd, List<object> ret)
        {
            sqlResultCacheMap[cmd] = ret;

            BDebug.Log("【添加缓存】 " + cmd);
        }

        /// <summary>
        /// 缓存列表
        /// </summary>
        public Dictionary<string, int> sqlExecCounterMap = new Dictionary<string, int>();

        /// <summary>
        /// 获取sql执行次数
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private int GetSqlExecCount(string cmd)
        {
            int counter = 0;
            var ret = sqlExecCounterMap.TryGetValue(cmd, out counter);
            if (!ret)
            {
                sqlExecCounterMap[cmd] = 0;
            }

            return counter;
        }

        /// <summary>
        /// 增加sql exec次数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="counter"></param>
        private void AddSqlExecCounter(string cmd, int counter = 0)
        {
            sqlExecCounterMap[cmd] = counter + 1;
        }

        #endregion

        /// <summary>
        /// 重置
        /// </summary>
        private void Reset()
        {
            this.@where = "";
            this.@sql = "";
            this.@limit = "";
        }
    }
}