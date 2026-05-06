using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using Cysharp.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;


namespace SQLite4Unity3d
{
    /// <summary>
    /// ILRuntime版本的TableQuery
    /// </summary>
    public class TableQueryForILRuntime : BaseTableQuery
    {
        public SQLiteConnection Connection { get; private set; }


        #region 语句缓存

        /// <summary>
        /// 控制是否在编辑器下打印 SQL 日志。基准测试期间可置为 false 以避免日志开销干扰计时。
        /// Control whether SQL logs are printed in the editor. Set to false during benchmarking
        /// to avoid log overhead interfering with timing.
        /// </summary>
        public static bool EnableEditorSqlLog = true;

        private string @where = "";
        private string @sql = "";
        private string @limit = "";

        /// <summary>
        /// SQL 语句执行计数器，用于判断是否触发 prepared statement 缓存
        /// 同时用于编辑器下的高频 SQL 告警
        /// SQL execution counter, used to determine whether to trigger prepared statement caching
        /// and for editor high-frequency SQL warnings
        /// </summary>
        Dictionary<string, int> sqlCmdCache = new Dictionary<string, int>();

        /// <summary>
        /// 是否启用 SQL 缓存
        /// </summary>
        private bool _sqlCacheEnabled = false;

        /// <summary>
        /// 触发缓存的最低执行次数
        /// </summary>
        private int _triggerCacheNum = 5;

        #endregion


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connection">sql连接器</param>
        /// <param name="triggerCacheNum">触发缓存次数</param>
        public TableQueryForILRuntime(SQLiteConnection connection)
        {
            this.Connection = connection;
        }

        /// <summary>
        /// 设置sql 缓存触发参数
        /// 当同一 SQL 语句执行次数超过 triggerCacheNum 时，自动缓存 prepared statement，
        /// 后续执行直接复用已编译的语句，跳过 Prepare 阶段（通常节省 30-50% 查询时间）。
        /// Set SQL cache trigger parameters.
        /// When the same SQL statement is executed more than triggerCacheNum times,
        /// the prepared statement is automatically cached for reuse, skipping Prepare phase
        /// (typically saving 30-50% query time).
        /// </summary>
        /// <param name="triggerCacheNum">触发缓存的最低执行次数 / Minimum execution count to trigger caching</param>
        /// <param name="triggerChacheTimer">（保留参数）触发缓存的时间阈值 / Reserved: time threshold for cache trigger</param>
        public void EnableSqlCahce(int triggerCacheNum = 5, float triggerChacheTimer = 0.05f)
        {
            _sqlCacheEnabled = true;
            _triggerCacheNum = triggerCacheNum;
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

            //重置状态
            this.@sql = "";
            this.@limit = "";
            this.@where = "";

            // SQL 执行频率追踪 — 编辑器和运行时均生效（ENABLE_BDEBUG 下零开销）
#if ENABLE_BDEBUG
            SqlitePerformanceMonitor.RecordQuery(sqlCmdText, 0, 0, 0);
#endif

#if UNITY_EDITOR
            if (BApplication.IsPlaying)
            {
                if (sqlCmdCache.ContainsKey(sqlCmdText))
                {
                    sqlCmdCache[sqlCmdText]++;
                }
                else
                {
                    sqlCmdCache.Add(sqlCmdText, 1);
                }
                var count = sqlCmdCache[sqlCmdText];
                if (count > 10)
                {
                    Debug.LogError($"Sql执行次数过多:<color=yellow>{count}</color>次,sql:" + sqlCmdText);
                }
            }
#endif

            return sqlCmdText;
        }

        #endregion


        /// <summary>
        /// 直接执行sql
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public TableQueryForILRuntime Exec(string sql)
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
        public TableQueryForILRuntime Where(string where, object value)
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
        public TableQueryForILRuntime Where(string where)
        {
            this.@where = ZString.Concat(this.@where, " ", where); 
            return this;
        }

        /// <summary>
        /// and语句
        /// </summary>
        /// <param name="where"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public TableQueryForILRuntime And
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
        public TableQueryForILRuntime Or
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
        public TableQueryForILRuntime WhereIn<T>(string field, IEnumerable<T> values)
        {
            string sqlcmd = "";
            if (typeof(T) == typeof(string))
            {
                sqlcmd = string.Join(",", values.Select(v => $"'{v}'"));
            }
            else
            {
                sqlcmd = string.Join(",", values);
            }
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlcmd);
            return this;
        }

        /// <summary>
        /// In语句查询
        /// </summary>
        public TableQueryForILRuntime WhereIn(string field, params object[] values)
        {
            string sqlcmd = "";
            if (values[0] is string)
            {
                sqlcmd = string.Join(",", values.Select(v => $"'{v}'"));
            }
            else
            {
                sqlcmd = string.Join(",", values);
            }
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlcmd);

            return this;
        }

        /// <summary>
        ///  where 语句
        /// </summary>
        public TableQueryForILRuntime WhereEqual(string where, object value)
        {
            string query;
            if (value is string)
            {
                query = ZString.Format("{0} = '{1}'", where, value);
            }
            else
            {
                query = ZString.Format("{0} = {1}", where, value);
            }
            this.@where = ZString.Concat(this.@where, " ", query);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryForILRuntime WhereOr(string field, string operation = "", params object[] objs)
        {
            string sqlcmd = "";
            for (int i = 0; i < objs.Length; i++)
            {
                var value = objs[i];
                if(value is string)
                {
                    value = ZString.Format("'{0}'", value);
                }
                if (string.IsNullOrEmpty(sqlcmd))
                {
                    sqlcmd = ZString.Format(" {0} {1} {2}", field, operation, value);
                }
                else
                {
                    sqlcmd += ZString.Format(" or {0} {1} {2}", field, operation, value);
                }
            }
            this.@where = sqlcmd;
            return this;
        }

        /// <summary>
        /// Where and
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TableQueryForILRuntime WhereAnd(string field, string operation = "", params object[] objs)
        {
            string sqlcmd = "";
            for (int i = 0; i < objs.Length; i++)
            {
                var value = objs[i];
                if(value is string)
                {
                    value = ZString.Format("'{0}'", value);
                }
                if (string.IsNullOrEmpty(sqlcmd))
                {
                    sqlcmd = ZString.Format(" {0} {1} {2}", field, operation, value);
                }
                else
                {
                    sqlcmd += ZString.Format(" and {0} {1} {2}", field, operation, value);
                }
            }

            this.@where = sqlcmd;
            return this;
        }

        #endregion

        #region Limit语句

        /// <summary>
        /// Limit 语句
        /// </summary>
        /// <param name="limitValue"></param>
        public TableQueryForILRuntime Limit(int limitValue)
        {
            this.limit = limitValue.ToString();

            return this;
        }

        #endregion

        #region 排序

        /// <summary>
        /// 降序排序
        /// </summary>
        public TableQueryForILRuntime OrderByDesc(string field)
        {
            var query = ZString.Format(" Order By {0} Desc", field);
            this.@where = ZString.Concat(this.@where, query);
            return this;
        }

        /// <summary>
        /// 升序排序
        /// </summary>
        public TableQueryForILRuntime OrderBy(string field)
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
            var ret = From(typeof(T), selection);
            return (T)ret;
        }

        public object From(Type type, string selection = "*")
        {
            var rets = this.Limit(1).FromAll(type, selection);

            if (rets.Count > 0)
            {
                return rets[0];
            }

            return null;
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
            var list = this.FromAll(typeof(T), selection);
 
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
        /// 支持自动 prepared statement 缓存：同一 SQL 执行超过阈值后，复用已编译语句
        /// </summary>
        /// <param name="type"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        public List<object> FromAll(Type type, string selection = "*")
        {
            var sqlCmdText = GenerateCommand(selection, type.Name);
#if UNITY_EDITOR
            if (EnableEditorSqlLog)
            {
                Debug.Log("sql:" + sqlCmdText);
            }
#endif
            // 查询：如果启用了缓存且该 SQL 已达到阈值，尝试从连接级缓存复用 prepared statement
            // Query: if caching is enabled and this SQL has reached the threshold, try reusing prepared statement from connection-level cache
            SQLiteCommand cmd;
            if (_sqlCacheEnabled
                && sqlCmdCache.TryGetValue(sqlCmdText, out var hitCount)
                && hitCount >= _triggerCacheNum)
            {
                var cachedStmt = this.Connection.GetPreparedStatement(sqlCmdText);
                if (cachedStmt != IntPtr.Zero)
                {
                    // 复用缓存的 prepared statement，跳过 Prepare() 编译阶段
                    cmd = this.Connection.CreateCommand(sqlCmdText);
                    cmd.SetPreparedStatement(cachedStmt);
                }
                else
                {
                    cmd = this.Connection.CreateCommand(sqlCmdText);
                }
            }
            else
            {
                cmd = this.Connection.CreateCommand(sqlCmdText);
            }

            var retlist = cmd.ExecuteQueryForILR(type);

            // 缓存首次达到阈值的 prepared statement 到连接级缓存
            // Cache the prepared statement to the connection-level cache when it first reaches the threshold
            if (_sqlCacheEnabled
                && this.Connection.GetPreparedStatement(sqlCmdText) == IntPtr.Zero
                && sqlCmdCache.TryGetValue(sqlCmdText, out var count2)
                && count2 >= _triggerCacheNum)
            {
                var stmt = cmd.GetPreparedStatement();
                if (stmt != IntPtr.Zero)
                {
                    this.Connection.SetPreparedStatement(sqlCmdText, stmt);
                }
            }

            return retlist;
        }

        #endregion
    }
}