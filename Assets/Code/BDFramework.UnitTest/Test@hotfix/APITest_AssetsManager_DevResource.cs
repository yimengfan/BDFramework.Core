using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.UnitTest;
using Game.Data;
using LitJson;
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
            o = BResources.Load<GameObject>("AssetTest/Particle");
            //不同的runtime目录
            o = BResources.Load<GameObject>("CubeSVN");
            o = BResources.Load<GameObject>("null");
            
        }
        
                
        [UnitTest(Des = "加载测试2")]
        static public void LoadALL()
        {
            //同个目录
            var objs = BResources.LoadALL<Sprite>("LoadAllTest/timg");
            Assert.Equals(objs.Length, 4);
            
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
            //寻找目录下整个
            var rets = BResources.ResLoader.GetAssets("AssetTest");
            Debug.Log(JsonMapper.ToJson(rets));
            Assert.Equals(rets.Length, 5);
            //寻找具体字符串
            var rets2 = BResources.ResLoader.GetAssets("AssetTest","Cu");
            Assert.Equals(rets2.Length, 1);
            Assert.Equals(rets2[0], "AssetTest/Cube","资源获取出错");
        }
        
    }
}