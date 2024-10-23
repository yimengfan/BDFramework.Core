using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Sql;
using LitJson;
using NUnit.Framework;
using SQLite4Unity3d;
using Unity.PerformanceTesting;
using UnityEngine;


namespace BDFramework.EditorTest
{
    /// <summary>
    /// Sqlite单元测试
    /// </summary>
    public class SqliteBenchmark
    {
        /// <summary>
        /// 单元测试的数据库
        /// </summary>
        private static string dbname = "Unitest_LiteDB_benchmark";

        /// <summary>
        /// 配置单元测试
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            int count = 100000;
            //构造插入数据
            var insertList = new List<UniTestSqliteType>();
            for (int i = 0; i < count; i++)
            {
                var t = new UniTestSqliteType() {Id = i};
                insertList.Add(t);
            }

            //创建测试db
            var dbpath = IPath.Combine(Application.persistentDataPath, dbname);
            if (File.Exists(dbpath))
            {
                File.Delete(dbpath);
            }

            Debug.Log("打开数据库:" + dbname);
            SqliteLoder.LoadDBReadWriteCreate(dbpath);
            //if (!ILRuntimeHelper.IsRunning)
            {
                //Drop table
                SqliteHelper.GetDB(dbname).CreateTable<UniTestSqliteType>();
                SqliteHelper.GetDB(dbname).InsertTable(insertList);
            }
            //var ret = SqliteHelper.GetDB(dbname).GetTableRuntime().FromAll<UniTestSqliteType>();
            Debug.Log($"<color=yellow>插入条目：{count}</color>");
            Assert.That(true);
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(1),Performance]
        static public void Old_Search_1000()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 1").And.Where("id <= 1000").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1000);
            Assert.AreEqual(ds[0].Id, 1);
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        // [Test, Order(1),Performance]
        static public void Old_Search_10000()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 1").And.Where("id <= 10000").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1000);
            Assert.AreEqual(ds[0].Id, 1);
        }

        /// <summary>
        /// 关闭数据库
        /// </summary>
        [OneTimeTearDown]
        static public void Close()
        {
            SqliteLoder.Close(dbname);
            var dbpath = IPath.Combine(Application.persistentDataPath, dbname);
            if (File.Exists(dbpath))
            {
                File.Delete(dbpath);
            }

            Debug.Log("关闭数据库:" + dbname);
        }
    }
}
