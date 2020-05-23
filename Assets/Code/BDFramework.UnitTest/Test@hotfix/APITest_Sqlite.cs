using System.Collections;
using System.Collections.Generic;
using BDFramework.Sql;
using BDFramework.UnitTest;
using Game.Data;
using LitJson;
using SQLite4Unity3d;
using UnityEngine;

namespace Tests
{
    [UnitTest(Des = "数据库测试")]
    static public class APITest_Sqlite
    {
        public class APITestHero
        {
            // id
            [PrimaryKey]
            public double Id { get; set; } = 1;

            // 名称
            public string Name { get; set; } = "xx";

            // 级别
            public string Level { get; set; } = "";

            // 星级 
            public double StarLevel { get; set; } = 1;

            // 下个等级
            public double NextLevel { get; set; } = 1;

            // 属性名
            public List<string> AttributeName { get; set; } = new List<string>();

            // 属性值
            public List<double> AttributeValue { get; set; } = new List<double>();

            // 拥有技能id
            public List<double> Skills { get; set; } = new List<double>();
        }

        [UnitTest(Des = "初始化数据库")]
        static public void Insert()
        {
            var h1 = new APITestHero() {Id = 1};
            var h2 = new APITestHero() {Id = 2};
            var h3 = new APITestHero() {Id = 3};

            if (SqliteLoder.Connection == null)
            {
                SqliteLoder.Load(Application.streamingAssetsPath);
            }
            
            SqliteHelper.Dbservice.CreateDB<APITestHero>();
            SqliteHelper.Dbservice.InsertTable(new List<APITestHero>(){h1,h2,h3});
        }

        [UnitTest(Des = "关闭",Order = 10000)]
        static public void Close()
        {
            SqliteLoder.Close();
        }
        
        
        [UnitTest(Des = "单条件查询")]
        static public void Select()
        {
            //单条件查询
            var ds = SqliteHelper.Dbservice.GetTableRuntime().Where("id = 1").ToSearch<APITestHero>();

            if (Assert.Equals(ds.Count, 1))
            {
                Assert.Equals(ds[0].Id, 1d);
            }
        }

        [UnitTest(Des = "多条件查询")]
        static public void Select_MultiCondition()
        {
            var ds = SqliteHelper.Dbservice.GetTableRuntime().Where("id > 1").Where("and id < 3").ToSearch<APITestHero>();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.Equals(ds.Count, 1);
            Assert.Equals(ds[0].Id, 2d);
        }


        [UnitTest(Des = "Where and查询")]
        static public void MultiSelect_WhereAnd()
        {
            var ds = SqliteHelper.Dbservice.GetTableRuntime().WhereAnd("id", "=", 1, 2).ToSearch<APITestHero>();
            Assert.Equals(ds.Count, 0);
        }

        [UnitTest(Des = "Where or查询")]
        static public void MultiSelect_WhereOr()
        {
            var ds = SqliteHelper.Dbservice.GetTableRuntime().WhereOr("id", "=", 2, 3).ToSearch<APITestHero>();

            Assert.Equals(ds.Count, 2);
            Assert.Equals(ds[0].Id, 2d);
            Assert.Equals(ds[1].Id, 3d);
        }








    }
}