using System;
using SQLite4Unity3d;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace BDFramework.Sql
{

    public class SQLiteService
    {
        //db connect
        public SQLiteConnection DB { get; private set; }

        public SQLiteService(string path)
        {
            DB = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            BDebug.Log("open db:" + path);
        }


        public void Close()
        {
            DB.Close();
        }

        public void CreateDB<T>()
        {
            DB.DropTable<T>();
            DB.CreateTable<T>();
        }

        public void CreateDBByType(Type t)
        {
            DB.DropTableByType(t);
            DB.CreateTableByType(t);
        }

        public void InsertTable(System.Collections.IEnumerable objects)
        {
            DB.InsertAll(objects);
        }


        /// <summary>
        /// 获取表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableQuery<T> GetTable<T>() where T : new()
        {
            return new TableQuery<T>(DB);
        }


        /// <summary>
        /// Runtime获取表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TableQueryILRuntime<T> GetTableRuntime<T>() where T : new()
        {
            return new TableQueryILRuntime<T>(DB);
        }
    }
}