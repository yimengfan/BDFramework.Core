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

        public SQLiteService(string dbName)
        {

            if (Application.isEditor)
            {
                dbName = Path.Combine(Application.streamingAssetsPath, dbName);
            }
            else
            {
                //非editor下在persistent目录下
                dbName = Path.Combine(Application.persistentDataPath, dbName);
            }


         
            DB = new SQLiteConnection(dbName, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            BDebug.Log("open db:" + dbName);
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