using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Sql;
using LitJson;
using NUnit.Framework;
using SQLite4Unity3d;
using Unity.PerformanceTesting;
using UnityEngine;


namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// Sqlite单元测试
    /// </summary>
    public class SqliteBenchmarkTest
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

        [Test, Order(0), Performance]
        static public void Search_1()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(1), Performance]
        static public void Search_100()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 1").And.Where("id <= 100").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 100);
            for (int i = 0; i < ds.Count; i++)
            {
                var d = ds[i];
                Assert.AreEqual(d.Id, 1 + i);
            }
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(2), Performance]
        static public void Search_1000()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 1001").And.Where("id <= 2000").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1000);
            for (int i = 0; i < ds.Count; i++)
            {
                var d = ds[i];
                Assert.AreEqual(d.Id, 1001 + i);
            }
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(3), Performance]
        static public void Search_10000()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 10001").And.Where("id <= 20000").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 10000);
            for (int i = 0; i < ds.Count; i++)
            {
                var d = ds[i];
                Assert.AreEqual(d.Id, 10001 + i);
            }
        }


        [Test, Order(10), Performance]
        static public void Fast_Search_1()
        {
            var tag = "Fast_Search_10000";
            BDebug.LogWatchBegin(tag);
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
            BDebug.LogWatchEnd(tag);
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(11), Performance]
        static public void Fast_Search_100()
        {
            var tag = "Fast_Search_10000";
            BDebug.LogWatchBegin(tag);
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 1").And.Where("id <= 100").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 100);
            for (int i = 0; i < ds.Count; i++)
            {
                var d = ds[i];
                Assert.AreEqual(d.Id, 1 + i);
            }
            BDebug.LogWatchEnd(tag);
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(12), Performance]
        static public void Fast_Search_1000()
        {
            var tag = "Fast_Search_10000";
            BDebug.LogWatchBegin(tag);
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 1001").And.Where("id <= 2000").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1000);
            for (int i = 0; i < ds.Count; i++)
            {
                var d = ds[i];
                Assert.AreEqual(d.Id, 1001 + i);
            }
            BDebug.LogWatchEnd(tag);
        }

        /// <summary>
        /// select语句
        /// </summary>
        [Test, Order(13), Performance]
        static public void Fast_Search_10000()
        {
            var tag = "Fast_Search_10000";
            BDebug.LogWatchBegin(tag);
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id >= 10001").And.Where("id <= 20000").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 10000);

            for (int i = 0; i < ds.Count; i++)
            {
                var d = ds[i];
                Assert.AreEqual(d.Id, 10001 + i);
            }
            BDebug.LogWatchEnd(tag);
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
