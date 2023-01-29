using System.Collections;
using System.Collections.Generic;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using BDFramework.UnitTest;
using BDFramework.UnitTest.Data;
using LitJson;
using UnityEngine;

namespace BDFramework.UnitTest
{
    [UnitTest(des:  "数据库测试")]
    static public class APITest_Sqlite
    {
        [UnitTest(des:  "初始化数据库")]
        static public void Insert()
        {
            //TODO 
            //暂时热更内不支持创建插入操作
            //该条测试可能会对后面有影响
            var insertList = new List<APITestHero>();
            for (int i = 0; i < 10000; i++)
            {
                var t = new APITestHero() {Id = i};
                insertList.Add(t);
            }

            if (!ILRuntimeHelper.IsRunning)
            {
                SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath,BApplication.RuntimePlatform);
                //Drop table
                SqliteHelper.DB.CreateTable<APITestHero>();
                SqliteHelper.DB.InsertTable(insertList);
                //
                var ret= SqliteHelper.DB.GetTableRuntime().FromAll<APITestHero>();
                Debug.Log($"<color=green>插入sql条目：{ret.Count}</color>");
            }
            
            Assert.IsPass(true);
        }


        [UnitTest(des:  "单条件查询")]
        static public void Select()
        {
            //单条件查询
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").FromAll<APITestHero>();
            var time = Assert.StopWatch();

            if (Assert.Equals(ds.Count, 1,time:time))
            {
                Assert.Equals(ds[0].Id, 1d,time: time);
            }
        }
        
        [UnitTest(des:  "Limit查询")]
        static public void Limit()
        {
            //单条件查询
            Assert.StartWatch();
            var d = SqliteHelper.DB.GetTableRuntime().Where("id != 1").Limit(1).From<APITestHero>();
            var time = Assert.StopWatch();
            if (d != null)
            {
                Debug.Log(JsonMapper.ToJson(d));
            }
            Assert.IsNull(d,"limit查询失败",time);
        }

        [UnitTest(des:  "Or And语句查询")]
        static public void Select_OR_And()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").And.Where("id < 3").FromAll<APITestHero>();
            var time = Assert.StopWatch();
            
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 1,time: time);
            Assert.Equals(ds[0].Id, 2d,time: time);
            //
            Assert.StartWatch();
            ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").Or.Where("id = 3").FromAll<APITestHero>();
            time = Assert.StopWatch();
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 2,time: time);
            Assert.Equals(ds[1].Id, 3d,time: time);
        }


        [UnitTest(des:  "Where and 批量查询")]
        static public void MultiSelect_WhereAnd()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).FromAll<APITestHero>();
            var time = Assert.StopWatch();
            
            Assert.Equals(ds.Count, 0,time: time);
        }

        [UnitTest(des:  "Where or 批量查询")]
        static public void MultiSelect_WhereOr()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).FromAll<APITestHero>();
            var time = Assert.StopWatch();
            
            Assert.Equals(ds.Count, 2,time: time);
            Assert.Equals(ds[0].Id, 2d,time: time);
            Assert.Equals(ds[1].Id, 3d,time: time);
        }
        
        [UnitTest(des:  "Where In 批量查询")]
        static public void MultiSelect_WhereIn()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().WhereIn("id", 2, 3).FromAll<APITestHero>();
            var time = Assert.StopWatch();
            
            Assert.Equals(ds.Count, 2,time: time);
            Assert.Equals(ds[0].Id, 2d,time: time);
            Assert.Equals(ds[1].Id, 3d,time: time);
        }

        [UnitTest(des:  "OrderByDesc 批量查询")]
        static public void MultiSelect_OrderByDesc()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().Where("Id >= 1").OrderByDesc("Id").FromAll<APITestHero>();
            var time = Assert.StopWatch();
            Debug.Log(JsonMapper.ToJson(ds));

            //降序检测
            bool isPass = true;
            for (int i = 0; i < ds.Count-1; i++)
            {
                if (ds[i].Id < ds[i + 1].Id)
                {
                    isPass = false;
                    break;
                }
            }
            Assert.IsPass(isPass,time:time);
        }
        
        [UnitTest(des:  "OrderBy 批量查询")]
        static public void MultiSelect_OrderBy()
        {
            Assert.StartWatch();
            var ds = SqliteHelper.DB.GetTableRuntime().Where("Id >= 1").OrderBy("Id").FromAll<APITestHero>();
            var time = Assert.StopWatch();
            
            Debug.Log(JsonMapper.ToJson(ds));
            //升序检测
            bool isPass = true;
            for (int i = 0; i < ds.Count-1; i++)
            {
                if (ds[i].Id > ds[i + 1].Id)
                {
                    isPass = false;
                    break;
                }
            }
            Assert.IsPass(isPass,time:time);
        }

        [UnitTest(10000, "关闭")]
        static public void Close()
        {
            if (!Application.isPlaying)
            {
                SqliteLoder.Close();
            }
        }
    }
}