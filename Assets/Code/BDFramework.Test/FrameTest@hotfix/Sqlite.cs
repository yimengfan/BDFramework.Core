using System.Collections;
using BDFramework.Sql;
using BDFramework.Test.hotfix;
using Game.Data;

namespace Tests
{
    [HotfixTest(Des ="数据库测试")]
    public class Sqlite
    {
        // A Test behaves as an ordinary method
        [HotfixTest]
        public void Select()
        {
            //单条件查询
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").ToSearch<Hero>();

            HotfixAssert.Equals(ds.Count, 1);
            HotfixAssert.Equals(ds[0].Id, 1);
            
            
        }

        [HotfixTest]
        public void Select_MultiCondition()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").Where("and id < 3").ToSearch<Hero>();

            HotfixAssert.Equals(ds.Count, 1);
            HotfixAssert.Equals(ds[0].Id, 2);
        }


        [HotfixTest]
        public void MultiSelect_WhereAnd()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).ToSearch<Hero>();
            HotfixAssert.Equals(ds.Count, 0);
        }
        
        [HotfixTest]
        public void MultiSelect_WhereOr()
        {
            var  ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).ToSearch<Hero>();
            
            HotfixAssert.Equals(ds.Count, 2);
            HotfixAssert.Equals(ds[0].Id, 2);
            HotfixAssert.Equals(ds[1].Id, 3);
        }
        
    }
}