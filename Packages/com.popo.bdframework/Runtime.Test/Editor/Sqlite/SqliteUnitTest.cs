using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Sql;
using LitJson;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;


namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// Sqlite单元测试
    /// </summary>
    public class SqliteUnitTest
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
                var t = new UniTestSqliteType()
                {
                    Id = i,
                    IdStr = i.ToString(),
                };
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


            Assert.That(true);
        }
        
        
        /// <summary>
        /// 数据库反序列化测试
        /// </summary>
        [Test, Order(1), Performance]
        static public void SqlItemDeSerializeTest()
        {
                        var rets = SqliteHelper.GetDB(dbname).GetTableRuntime().FromAll<UniTestSqliteType>();
            Debug.Log($"<color=yellow>插入条目：{rets.Count}</color>");
            //反序列化判断
            var source = new UniTestSqliteType() { };
            foreach (var item in rets)
            {
                //判断是否相等
                var ret = source.TestBool.Equals(item.TestBool);
                if (!ret)
                {
                    Assert.Fail("Bool类型校验失败");
                }
                ret = source.TestInt.Equals(item.TestInt);
                if (!ret)
                {
                    Assert.Fail("Int类型校验失败");
                }
                ret = source.TestString.Equals(item.TestString);
                if (!ret)
                {
                    Assert.Fail("String类型校验失败");
                }
                ret = source.TestFloat - item.TestFloat < Double.Epsilon;
                if (!ret)
                {
                    Assert.Fail("Float类型校验失败");
                }
                ret = source.TestDouble - item.TestDouble < Double.Epsilon;
                if (!ret)
                {
                    Assert.Fail("Double类型校验失败");
                }
                //数组判断相等
                var boolExceptArray = source.TestBoolArray.Except(item.TestBoolArray);
                if (boolExceptArray.Count() != 0)
                {
                    Assert.Fail("BoolArray类型校验失败");
                }
                var intExceptArray = source.TestIntArray.Except(item.TestIntArray);
                if (intExceptArray.Count() != 0)
                {
                    Assert.Fail("intArray类型校验失败");
                }
                var stringExceptArray = source.TestStringArray.Except(item.TestStringArray);
                if (stringExceptArray.Count() != 0)
                {
                    Assert.Fail("stringArray类型校验失败");
                }

                for (int i = 0; i < source.TestFloatArray.Length; i++)
                {
                    ret = source.TestFloatArray[i] - item.TestFloatArray[i] < Double.Epsilon;
                    if (!ret)
                    {
                        Assert.Fail("FloatArray类型校验失败");
                    }
                }
                
                for (int i = 0; i < source.TestDoubleArray.Length; i++)
                {
                    ret = source.TestDoubleArray[i] - item.TestDoubleArray[i] < Double.Epsilon;
                    if (!ret)
                    {
                        Assert.Fail("DoubleArray类型校验失败");
                    }
                }
            }

        }
        
        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(1), Performance]
        static public void Where()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        [Test, Order(1), Performance]
        static public void Where_String()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("idstr = '1'").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        
        [Test, Order(1), Performance]
        static public void WhereEqual()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereEqual("id",1).FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        [Test, Order(1), Performance]
        static public void WhereEqual_String()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereEqual("idstr","1").FromAll<UniTestSqliteType>();
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
        
        [Test, Order(3), Performance]
        static public void MultiResult_OR_And_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id == 2").And.Where("idstr == '2'")
                .FromAll<UniTestSqliteType>();
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 2);
            //

            ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("idstr = '1'").Or.Where("idstr = '3'")
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
        [Test, Order(4), Performance]
        static public void MultiResult_WhereAnd_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereAnd("idstr", "=", "1", "2")
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
        [Test, Order(5), Performance]
        static public void MultiResult_WhereOr_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereOr("idstr", "=", "2", "3")
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

        [Test, Order(6), Performance]
        static public void MultiResult_WhereIn_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereIn("idstr", "2", "3").FromAll<UniTestSqliteType>();
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
