using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using Cysharp.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Sqlite3Statement = System.IntPtr;

namespace SQLite4Unity3d
{
    /// <summary>
    /// 旧运行时查询接口兼容层。
    /// 保留 TableQueryForILRuntime 的链式 API，内部映射到当前 SQLiteCommand/SQLiteConnection 查询管线。
    /// Legacy runtime query API compatibility layer.
    /// Preserves the TableQueryForILRuntime fluent API while mapping internally to the current
    /// SQLiteCommand/SQLiteConnection query pipeline.
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
        /// SQL 语句执行计数器，用于判断是否触发 prepared statement 缓存，
        /// 同时用于编辑器下的高频 SQL 告警。
        /// SQL execution counter used to determine whether prepared statement caching should kick in,
        /// and to warn about high-frequency SQL in the editor.
        /// </summary>
        private readonly Dictionary<string, int> sqlCmdCache = new Dictionary<string, int>();

        /// <summary>
        /// 是否启用 SQL 缓存。
        /// Whether prepared statement caching is enabled.
        /// </summary>
        private bool _sqlCacheEnabled;

        /// <summary>
        /// 触发缓存的最低执行次数。
        /// Minimum execution count required before caching a prepared statement.
        /// </summary>
        private int _triggerCacheNum = 5;

        #endregion

        /// <summary>
        /// 构造函数。
        /// Constructor.
        /// </summary>
        /// <param name="connection">sql连接器 / SQL connection</param>
        public TableQueryForILRuntime(SQLiteConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// 设置 sql 缓存触发参数。
        /// 当同一 SQL 语句执行次数超过 triggerCacheNum 时，自动缓存 prepared statement，
        /// 后续执行直接复用已编译语句，跳过 Prepare 阶段。
        /// Set sql cache trigger parameters.
        /// When the same SQL statement is executed more than triggerCacheNum times,
        /// the prepared statement is cached automatically and reused on later executions,
        /// skipping the Prepare phase.
        /// </summary>
        /// <param name="triggerCacheNum">触发缓存的最低执行次数 / Minimum execution count to trigger caching</param>
        /// <param name="triggerChacheTimer">保留参数 / Reserved parameter</param>
        public void EnableSqlCahce(int triggerCacheNum = 5, float triggerChacheTimer = 0.05f)
        {
            _sqlCacheEnabled = true;
            _triggerCacheNum = triggerCacheNum;
        }

        #region 生成sql cmd

        private string GenerateCommand(string @select, string tablename)
        {
            string sqlCmdText;

            if (string.IsNullOrEmpty(@sql))
            {
                sqlCmdText = ZString.Format("select {0} from {1}", @select, tablename);

                if (!string.IsNullOrEmpty(@where))
                {
                    sqlCmdText = ZString.Concat(sqlCmdText, " where", @where);
                }

                if (!string.IsNullOrEmpty(@limit))
                {
                    sqlCmdText = ZString.Concat(sqlCmdText, " Limit ", @limit);
                }
            }
            else
            {
                sqlCmdText = @sql;
            }

            @sql = "";
            @limit = "";
            @where = "";

#if ENABLE_BDEBUG
            SqlitePerformanceMonitor.RecordQuery(sqlCmdText, 0, 0, 0);
#endif

#if UNITY_EDITOR
            if (BApplication.IsPlaying)
            {
                if (sqlCmdCache.TryGetValue(sqlCmdText, out var count))
                {
                    sqlCmdCache[sqlCmdText] = count + 1;
                }
                else
                {
                    sqlCmdCache.Add(sqlCmdText, 1);
                }

                if (sqlCmdCache[sqlCmdText] > 10)
                {
                    Debug.LogError($"Sql执行次数过多:<color=yellow>{sqlCmdCache[sqlCmdText]}</color>次,sql:{sqlCmdText}");
                }
            }
#endif

            return sqlCmdText;
        }

        #endregion

        /// <summary>
        /// 直接执行 sql。
        /// Set raw sql text directly.
        /// </summary>
        public TableQueryForILRuntime Exec(string sql)
        {
            @sql = sql;
            return this;
        }

        #region Where、Or、And、Limit

        /// <summary>
        /// Where 语句。
        /// Append a formatted WHERE clause fragment.
        /// </summary>
        public TableQueryForILRuntime Where(string where, object value)
        {
            if (value is string)
            {
                value = ZString.Format("'{0}'", value);
            }

            @where = ZString.Concat(@where, " ", ZString.Format(where, value));
            return this;
        }

        /// <summary>
        /// Where 语句。
        /// Append a raw WHERE clause fragment.
        /// </summary>
        public TableQueryForILRuntime Where(string where)
        {
            @where = ZString.Concat(@where, " ", where);
            return this;
        }

        /// <summary>
        /// and 语句。
        /// Append an AND operator.
        /// </summary>
        public TableQueryForILRuntime And
        {
            get
            {
                @where = ZString.Concat(@where, " and");
                return this;
            }
        }

        /// <summary>
        /// Or 语句。
        /// Append an OR operator.
        /// </summary>
        public TableQueryForILRuntime Or
        {
            get
            {
                @where = ZString.Concat(@where, " or");
                return this;
            }
        }

        /// <summary>
        /// In 语句查询。
        /// Build an IN clause from a typed collection.
        /// </summary>
        public TableQueryForILRuntime WhereIn<T>(string field, IEnumerable<T> values)
        {
            var sqlcmd = typeof(T) == typeof(string)
                ? string.Join(",", values.Select(v => $"'{v}'"))
                : string.Join(",", values);

            @where = ZString.Format("{0} {1} in ({2})", @where, field, sqlcmd);
            return this;
        }

        /// <summary>
        /// In 语句查询。
        /// Build an IN clause from object params.
        /// </summary>
        public TableQueryForILRuntime WhereIn(string field, params object[] values)
        {
            if (values == null || values.Length == 0)
            {
                return this;
            }

            var sqlcmd = values[0] is string
                ? string.Join(",", values.Select(v => $"'{v}'"))
                : string.Join(",", values);

            @where = ZString.Format("{0} {1} in ({2})", @where, field, sqlcmd);
            return this;
        }

        /// <summary>
        /// where = value 语句。
        /// Build an equality comparison fragment.
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

            @where = ZString.Concat(@where, " ", query);
            return this;
        }

        /// <summary>
        /// Where or。
        /// Build OR-connected comparisons for the same field.
        /// </summary>
        public TableQueryForILRuntime WhereOr(string field, string operation = "", params object[] objs)
        {
            string sqlcmd = "";
            for (int i = 0; i < objs.Length; i++)
            {
                var value = objs[i];
                if (value is string)
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

            @where = sqlcmd;
            return this;
        }

        /// <summary>
        /// Where and。
        /// Build AND-connected comparisons for the same field.
        /// </summary>
        public TableQueryForILRuntime WhereAnd(string field, string operation = "", params object[] objs)
        {
            string sqlcmd = "";
            for (int i = 0; i < objs.Length; i++)
            {
                var value = objs[i];
                if (value is string)
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

            @where = sqlcmd;
            return this;
        }

        #endregion

        #region Limit语句

        /// <summary>
        /// Limit 语句。
        /// Set the LIMIT clause.
        /// </summary>
        public TableQueryForILRuntime Limit(int limitValue)
        {
            @limit = limitValue.ToString();
            return this;
        }

        #endregion

        #region 排序

        /// <summary>
        /// 降序排序。
        /// Append a descending ORDER BY clause.
        /// </summary>
        public TableQueryForILRuntime OrderByDesc(string field)
        {
            var query = ZString.Format(" Order By {0} Desc", field);
            @where = ZString.Concat(@where, query);
            return this;
        }

        /// <summary>
        /// 升序排序。
        /// Append an ascending ORDER BY clause.
        /// </summary>
        public TableQueryForILRuntime OrderBy(string field)
        {
            var query = ZString.Format(" Order By {0}", field);
            @where = ZString.Concat(@where, query);
            return this;
        }

        #endregion

        #region Select、From语句

        /// <summary>
        /// 查询单条数据。
        /// Query a single row using the current builder state.
        /// </summary>
        public T From<T>(string selection = "*")
        {
            var ret = From(typeof(T), selection);
            return (T)ret;
        }

        public object From(Type type, string selection = "*")
        {
            var rets = Limit(1).FromAll(type, selection);

            if (rets.Count > 0)
            {
                return rets[0];
            }

            return null;
        }

        /// <summary>
        /// 查询所有数据。
        /// Query all rows using the current builder state.
        /// </summary>
        public List<T> FromAll<T>(string selection = "*")
        {
            var list = FromAll(typeof(T), selection);
            var retList = new List<T>(list.Count);

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
        /// 非泛型查询。
        /// 保留旧兼容接口，内部复用当前 TableMapping + ExecuteQuery 路径。
        /// Non-generic query.
        /// Preserves the legacy compatibility API while reusing the current TableMapping + ExecuteQuery path.
        /// </summary>
        public List<object> FromAll(Type type, string selection = "*")
        {
            var sqlCmdText = GenerateCommand(selection, type.Name);
#if UNITY_EDITOR
            if (EnableEditorSqlLog)
            {
                Debug.Log("sql:" + sqlCmdText);
            }
#endif

            var mapping = Connection.GetMapping(type);
            SQLiteCommand cmd;
            if (_sqlCacheEnabled
                && sqlCmdCache.TryGetValue(sqlCmdText, out var hitCount)
                && hitCount >= _triggerCacheNum)
            {
                var cachedStmt = Connection.GetPreparedStatement(sqlCmdText);
                if (cachedStmt != IntPtr.Zero)
                {
                    cmd = Connection.CreateCommand(sqlCmdText);
                    cmd.SetPreparedStatement(cachedStmt);
                }
                else
                {
                    cmd = Connection.CreateCommand(sqlCmdText);
                }
            }
            else
            {
                cmd = Connection.CreateCommand(sqlCmdText);
            }

            var retlist = cmd.ExecuteQuery<object>(mapping);

            if (_sqlCacheEnabled
                && Connection.GetPreparedStatement(sqlCmdText) == IntPtr.Zero
                && sqlCmdCache.TryGetValue(sqlCmdText, out var count)
                && count >= _triggerCacheNum)
            {
                Sqlite3Statement stmt = cmd.GetPreparedStatement();
                if (stmt != IntPtr.Zero)
                {
                    Connection.SetPreparedStatement(sqlCmdText, stmt);
                }
            }

            return retlist;
        }

        #endregion
    }
}
