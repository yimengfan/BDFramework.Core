using System;
using SQLite4Unity3d;

namespace BDFramework.Sql
{
    public class SQLiteService
    {
        //db connect
        public SQLiteConnection Connection { get; private set; }

        public SQLiteService(SQLiteConnection con)
        {
            this.Connection = con;
        }


        public void Close()
        {
            if (Connection != null)
                Connection.Close();
        }

        public void CreateDB<T>()
        {
            Connection.DropTable<T>();
            Connection.CreateTable<T>();
        }

        public void CreateDBByType(Type t)
        {
            Connection.DropTableByType(t);
            Connection.CreateTableByType(t);
        }

        public void InsertTable(System.Collections.IEnumerable objects)
        {
            Connection.InsertAll(objects);
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