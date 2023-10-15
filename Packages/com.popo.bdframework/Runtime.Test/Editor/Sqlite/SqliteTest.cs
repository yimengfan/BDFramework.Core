using System.Collections.Generic;
using System.IO;
using BDFramework.Sql;
using LitJson;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;


namespace BDFramework.EditorTest
{
    /// <summary>
    /// Sqlite单元测试
    /// </summary>
    public class SqliteTest
    {
        /// <summary>
        /// 单元测试的数据库
        /// </summary>
        private static string dbname = "Unitest_LiteDB";

        /// <summary>
        /// 配置单元测试
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            //构造插入数据
            var insertList = new List<UniTestSqliteType>();
            for (int i = 0; i < 1000; i++)
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
            var ret = SqliteHelper.GetDB(dbname).GetTableRuntime().FromAll<UniTestSqliteType>();
            Debug.Log($"<color=yellow>插入条目：{ret.Count}</color>");
            Assert.That(true);
        }

        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(1),Performance]
        static public void Where()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }

        /// <summary>
        /// limit语句
        /// </summary>
        [Test, Order(2), Performance]
        static public void Limit()
        {
            //单条件查询
            var d = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id != 1").Limit(1)
                .From<UniTestSqliteType>();
            if (d != null)
            {
                Debug.Log(JsonMapper.ToJson(d));
            }

            Assert.True(d != null && d.Id != 1);
        }

        /// <summary>
        /// 选择、or、and语句
        /// </summary>
        [Test, Order(3), Performance]
        static public void MultiResult_OR_And()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id > 1").And.Where("id < 3")
                .FromAll<UniTestSqliteType>();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 2);
            //

            ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").Or.Where("id = 3")
                .FromAll<UniTestSqliteType>();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 1);
            Assert.AreEqual(ds[1].Id, 3);
        }


        /// <summary>
        /// 多返回Whereand语句
        /// </summary>
        [Test, Order(4), Performance]
        static public void MultiResult_WhereAnd()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereAnd("id", "=", 1, 2)
                .FromAll<UniTestSqliteType>();
            Assert.AreEqual(ds.Count, 0);
        }

        /// <summary>
        /// where or语句
        /// </summary>
        [Test, Order(5), Performance]
        static public void MultiResult_WhereOr()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereOr("id", "=", 2, 3)
                .FromAll<UniTestSqliteType>();

            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }

        /// <summary>
        /// 多返回 Whre in 语句
        /// </summary>
        [Test, Order(6), Performance]
        static public void MultiResult_WhereIn()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereIn("id", 2, 3).FromAll<UniTestSqliteType>();
            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }


        /// <summary>
        /// 多返回-排序
        /// </summary>
        [Test, Order(7), Performance]
        static public void MultiResult_OrderByDesc()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("Id >= 0").OrderByDesc("Id")
                .FromAll<UniTestSqliteType>();

            //降序检测
            bool isPass = true;
            for (int i = 0; i < ds.Count - 1; i++)
            {
                if (ds[i].Id < ds[i + 1].Id)
                {
                    isPass = false;
                    break;
                }
            }

            Assert.True(isPass);
        }


        /// <summary>
        /// 多返回并且排序
        /// </summary>
        [Test, Order(8), Performance]
        static public void MultiResult_OrderBy()
        {
            BDebug.LogWatchBegin("order by");
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("Id >= 0").OrderBy("Id")
                .FromAll<UniTestSqliteType>();
            BDebug.LogWatchEnd("order by");

            //升序检测
            bool isPass = true;
            for (int i = 0; i < ds.Count - 1; i++)
            {
                if (ds[i].Id > ds[i + 1].Id)
                {
                    isPass = false;
                    break;
                }
            }

            Assert.True(isPass);
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
