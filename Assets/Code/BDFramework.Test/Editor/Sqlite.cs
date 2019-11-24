using System.Collections;
using BDFramework.Sql;
using Game.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class Sqlite
    {
        // A Test behaves as an ordinary method
        [Test, Order(1)]
        public void Select()
        {
            //单条件查询
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").ToSearch<Hero>();

            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }

        [Test, Order(2)]
        public void Select_MultiCondition()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").Where("and id < 3").ToSearch<Hero>();

            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 2);
        }


        [Test, Order(3)]
        public void MultiSelect_WhereAnd()
        {
            var ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).ToSearch<Hero>();
            Assert.AreEqual(ds.Count, 0);
        }
        
        [Test, Order(4)]
        public void MultiSelect_WhereOr()
        {
            var  ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).ToSearch<Hero>();
            Assert.AreEqual(ds.Count, 2);
            
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }
        
    }
}