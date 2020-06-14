using System;
using System.Collections.Generic;
using SQLite4Unity3d;

namespace BDFramework.Sql
{
    public class SQLiteService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="con"></param>
        public SQLiteService(SQLiteConnection con)
        {
            this.Connection = con;
        }

        //db connect
        private SQLiteConnection Connection { get; set; }


        /// <summary>
        /// 是否关闭
        /// </summary>
        public bool IsClose
        {
            get
            {
                return Connection == null || !Connection.IsOpen;
            }
        }

        /// <summary>
        /// 创建db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CreateDB<T>()
        {
            Connection.DropTable<T>();
            Connection.CreateTable<T>();
        }

        /// <summary>
        /// 创建db
        /// </summary>
        /// <param name="t"></param>
        public void CreateDB(Type t)
        {
            Connection.DropTableByType(t);
            Connection.CreateTableByType(t);
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
            Connection.Insert(@obj,typeof(T));
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
        /// Runtime获取表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableQueryILRuntime GetTableRuntime()
        {
            var table = new TableQueryILRuntime(this.Connection);
            return table;
        }
    }
}