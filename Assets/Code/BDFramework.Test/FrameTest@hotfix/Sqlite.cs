using System.Collections;
using BDFramework.Sql;
using BDFramework.UnitTest;
using Game.Data;
using UnityEngine;

namespace Tests
{
    [HotfixTest(Des ="数据库测试")]
    static  public class Sqlite
    {
        /// <summary>
        /// 打开数据库 
        /// </summary>
        [HotfixTest(Order = -100)]
        static public void OpenSqlite()
        {
            SqliteLoder.Load(Application.streamingAssetsPath);
        }
        /// <summary>
        /// 关闭数据库
        /// </summary>
        [HotfixTest(Order = 100)]
        static public void CloseSqlite()
        {
            SqliteLoder.Connection.Dispose();
        }
        // A Test behaves as an ordinary method
        [HotfixTest]
        static public void Select()
        {
            //单条件查询
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").ToSearch<Hero>();

            if (Assert.Equals(ds.Count, 1))
            {
                Assert.Equals(ds[0].Id, 1d);
            }
        }

        [HotfixTest]
        static  public void Select_MultiCondition()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").Where("and id < 3").ToSearch<Hero>();

            Assert.Equals(ds.Count, 1);
            Assert.Equals(ds[0].Id, 2d);
        }


        [HotfixTest]
        static public void MultiSelect_WhereAnd()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).ToSearch<Hero>();
            Assert.Equals(ds.Count, 0);
        }
        
        [HotfixTest]
        static public void MultiSelect_WhereOr()
        {
            var  ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).ToSearch<Hero>();
            
            Assert.Equals(ds.Count, 2);
            Assert.Equals(ds[0].Id, 2d);
            Assert.Equals(ds[1].Id, 3d);
        }
        
    }
}