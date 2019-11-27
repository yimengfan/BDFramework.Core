using System.Collections;
using BDFramework.Sql;
using BDFramework.Test.hotfix;
using Game.Data;

namespace Tests
{
    public class Sqlite
    {
        // A Test behaves as an ordinary method
        [BDTest]
        public void Select(TestResult ret)
        {
            //单条件查询
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").ToSearch<Hero>();

            ret.Equals(ds.Count, 1);
            ret.Equals(ds[0].Id, 1);
        }

        [BDTest]
        public void Select_MultiCondition(TestResult ret)
        {
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").Where("and id < 3").ToSearch<Hero>();

            ret.Equals(ds.Count, 1);
            ret.Equals(ds[0].Id, 2);
        }


        [BDTest]
        public void MultiSelect_WhereAnd(TestResult ret)
        {
            var ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).ToSearch<Hero>();
            ret.Equals(ds.Count, 0);
        }
        
        [BDTest]
        public void MultiSelect_WhereOr(TestResult ret)
        {
            var  ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).ToSearch<Hero>();
            
            ret.Equals(ds.Count, 2);
            ret.Equals(ds[0].Id, 2);
            ret.Equals(ds[1].Id, 3);
        }
        
    }
}