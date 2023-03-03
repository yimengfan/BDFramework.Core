using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using BDFramework.UnitTest;
using BDFramework.UnitTest.Data;
using LitJson;
using UnityEngine;

namespace BDFramework.UnitTest
{
    [UnitTest(des: "数据库测试")]
    static public class APITest_Sqlite
    {
        private static string dbname = "UnitestDB";

        [UnitTest(des: "初始化数据库")]
        static public void Insert()
        {
            //构造插入数据
            var insertList = new List<UniTestSqlite_AllType>();
            for (int i = 0; i < 10000; i++)
            {
                var t = new UniTestSqlite_AllType() { Id = i };
                insertList.Add(t);
            }

            //创建测试db
            var dbpath = IPath.Combine(Application.persistentDataPath, dbname);
            if (File.Exists(dbpath))
            {
                File.Delete(dbpath);
            }
            SqliteLoder.LoadDBReadWriteCreate(dbpath);
            //if (!ILRuntimeHelper.IsRunning)
            {
                //Drop table
                SqliteHelper.GetDB(dbname).CreateTable<UniTestSqlite_AllType>();
                SqliteHelper.GetDB(dbname).InsertTable(insertList);
            }
            var ret = SqliteHelper.GetDB(dbname).GetTableRuntime().FromAll<UniTestSqlite_AllType>();
            Debug.Log($"<color=yellow>插入条目：{ret.Count}</color>");
            Assert.IsPass(true);
        }

        [UnitTest(des: "单条件查询")]
        static public void Select()
        {
            //单条件查询
            Assert.StartWatch();
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").FromAll<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();

            //对比返回数量和id
            if (Assert.Equals(ds.Count, 1, time: time))
            {
                Assert.Equals(ds[0].Id, 1, time: time);
            }
        }

        [UnitTest(des: "Limit查询")]
        static public void Limit()
        {
            //单条件查询
            Assert.StartWatch();
            var d = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id != 1").Limit(1)
                .From<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();
            if (d != null)
            {
                Debug.Log(JsonMapper.ToJson(d));
            }

            Assert.IsPass(d != null && d.Id != 1, time: time);
        }

        [UnitTest(des: "Or And语句查询")]
        static public void Select_OR_And()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id > 1").And.Where("id < 3")
                .FromAll<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 1, time: time);
            Assert.Equals(ds[0].Id, 2, time: time);
            //
            Assert.StartWatch();
            ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").Or.Where("id = 3")
                .FromAll<UniTestSqlite_AllType>();
            time = Assert.StopWatch();
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 2, time: time);
            Assert.Equals(ds[0].Id, 1, time: time);
            Assert.Equals(ds[1].Id, 3, time: time);
        }


        [UnitTest(des: "Where and 批量查询")]
        static public void MultiSelect_WhereAnd()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereAnd("id", "=", 1, 2)
                .FromAll<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();

            Assert.Equals(ds.Count, 0, time: time);
        }

        [UnitTest(des: "Where or 批量查询")]
        static public void MultiSelect_WhereOr()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereOr("id", "=", 2, 3)
                .FromAll<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();

            Assert.Equals(ds.Count, 2, time: time);
            Assert.Equals(ds[0].Id, 2, time: time);
            Assert.Equals(ds[1].Id, 3, time: time);
        }

        [UnitTest(des: "Where In 批量查询")]
        static public void MultiSelect_WhereIn()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereIn("id", 2, 3).FromAll<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();

            Assert.Equals(ds.Count, 2, time: time);
            Assert.Equals(ds[0].Id, 2, time: time);
            Assert.Equals(ds[1].Id, 3, time: time);
        }

        [UnitTest(des: "OrderByDesc 批量查询")]
        static public void MultiSelect_OrderByDesc()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("Id >= 0").OrderByDesc("Id")
                .FromAll<UniTestSqlite_AllType>();
            var time = Assert.StopWatch();
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

            Assert.IsPass(isPass, time: time);
        }

        [UnitTest(des: "OrderBy 批量查询")]
        static public void MultiSelect_OrderBy()
        {
            Assert.StartWatch();
            BDebug.LogWatchBegin("order by");
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("Id >= 0").OrderBy("Id")
                .FromAll<UniTestSqlite_AllType>();
            BDebug.LogWatchEnd("order by");
            var time = Assert.StopWatch();

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

            Assert.IsPass(isPass, time: time);
        }

        [UnitTest(10000, "关闭")]
        static public void Close()
        {
            SqliteLoder.Close(dbname);
            var dbpath = IPath.Combine(Application.persistentDataPath, dbname);
            // File.Delete(dbpath);
        }
    }
}