using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.UnitTest;
using Game.Data;
using UnityEngine;

namespace Code.BDFramework.UnitTest
{
    [UnitTestAttribute(Des = "资源管理测试-DevResource")]
    static public class APITest_AssetsManager_DevResource
    {
        
        
        [UnitTest(Des = "初始化")]
        static public void Register()
        {
            BResources.Load();
        }
        
        
        
        [UnitTest(Des = "加载测试")]
        static public void Load()
        {
            //同个目录
            var o = BResources.Load<GameObject>("AssetTest/Cube");
            GameObject.Instantiate(o);
            o = BResources.Load<GameObject>("AssetTest/Particle");
            GameObject.Instantiate(o);
            //不同的runtime目录
            o = BResources.Load<GameObject>("CubeSVN");
            GameObject.Instantiate(o);
            
        }
        
        [UnitTest(Des = "异步测试")]
        static public void AsyncLoad()
        {
        }
        
        [UnitTest(Des = "批量加载测试")]
        static public void MultipleLoad()
        {
        }

        
        [UnitTest(Des = "路径获取测试")]
        static public void GetAssets()
        {
            var rets = BResources.ResLoader.GetAssets("AssetTest");
            Assert.Equals(rets.Length, 5);
            var rets2 = BResources.ResLoader.GetAssets("AssetTest","Cu");
            Assert.Equals(rets2.Length, 1);
        }
        
    }
}