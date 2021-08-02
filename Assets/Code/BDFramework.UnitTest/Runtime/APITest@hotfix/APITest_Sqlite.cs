using System.Collections;
using System.Collections.Generic;
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
            var h1 = new APITestHero() {Id = 1};
            var h2 = new APITestHero() {Id = 2};
            var h3 = new APITestHero() {Id = 3};

            if (!ILRuntimeHelper.IsRunning)
            {
                SqliteLoder.LoadLocalDBOnEditor(Application.streamingAssetsPath,Application.platform);
                SqliteHelper.DB.CreateTable<APITestHero>();
                SqliteHelper.DB.InsertTable(new List<APITestHero>() {h1, h2, h3});
            }
            
            Assert.IsPass(true);
        }


        [UnitTest(des:  "单条件查询")]
        static public void Select()
        {
            //单条件查询
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").FromAll<APITestHero>();

            if (Assert.Equals(ds.Count, 1))
            {
                Assert.Equals(ds[0].Id, 1d);
            }
        }
        
        [UnitTest(des:  "Limit查询")]
        static public void Limit()
        {
            //单条件查询
            var d = SqliteHelper.DB.GetTableRuntime().Where("id != 1").Limit(1).From<APITestHero>();
            if (d != null)
            {
                Debug.Log(JsonMapper.ToJson(d));
            }
            Assert.IsNull(d,"limit查询失败");
        }

        [UnitTest(des:  "Or And语句查询")]
        static public void Select_OR_And()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").And.Where("id < 3").FromAll<APITestHero>();
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 1);
            Assert.Equals(ds[0].Id, 2d);
            
            ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").Or.Where("id = 3").FromAll<APITestHero>();
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 2);
            Assert.Equals(ds[1].Id, 3d);
        }


        [UnitTest(des:  "Where and 批量查询")]
        static public void MultiSelect_WhereAnd()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).FromAll<APITestHero>();
            Assert.Equals(ds.Count, 0);
        }

        [UnitTest(des:  "Where or 批量查询")]
        static public void MultiSelect_WhereOr()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).FromAll<APITestHero>();

            Assert.Equals(ds.Count, 2);
            Assert.Equals(ds[0].Id, 2d);
            Assert.Equals(ds[1].Id, 3d);
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