using System;
using System.Collections.Generic;
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

        private string @where = "";
        private string @sql = "";
        private string @limit = "";

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
        /// </summary>
        /// <param name="triggerCacheNum"></param>
        /// <param name="triggerChacheTimer"></param>
        public void EnableSqlCahce(int triggerCacheNum = 5, float triggerChacheTimer = 0.05f)
        {
        }

        #region 生成sql cmd
        
        Dictionary<string ,int> sqlCmdCache = new Dictionary<string, int>();

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


#if UNITY_EDITOR
            if (Application.isPlaying)
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
                if (count > 2)
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
            this.@where = ZString.Concat(this.@where, " ", where); // string.Format((" " + where), where);
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
            var sqlIn = string.Join(",", values);
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlIn);

            return this;
        }

        /// <summary>
        /// In语句查询
        /// </summary>
        public TableQueryForILRuntime WhereIn(string field, params object[] objs)
        {
            var sqlIn = string.Join(",", objs);
            this.@where = ZString.Format("{0} {1} in ({2})", this.@where, field, sqlIn);

            return this;
        }

        /// <summary>
        /// 仿MyBatis CRUD 更符合nameof规范
        /// </summary>
        public TableQueryForILRuntime WhereEqual(string where, object value)
        {
            var query = ZString.Format("{0} = {1}", where, value);
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
        public TableQueryForILRuntime WhereAnd(string field, string operation = "", params object[] objs)
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
        /// </summary>
        /// <param name="type"></param>
        /// <param name="selection"></param>
        /// <returns></returns>
        public List<object> FromAll(Type type, string selection = "*")
        {
            var sqlCmdText = GenerateCommand(selection, type.Name);
#if UNITY_EDITOR
            Debug.Log("sql:" + sqlCmdText);
#endif
            //查询
            var cmd = this.Connection.CreateCommand(sqlCmdText);
            var retlist = cmd.ExecuteQueryForILR(type);
            return retlist;
        }

        #endregion
    }
}