using System;
using BDFramework;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.UnitTest;
using LitJson;
using UnityEngine;

namespace BDFramework.UnitTest
{
    [UnitTest(des:  "资源管理测试-DevResource")]
    static public class APITest_AssetsManager_DevResource
    {
        
        
        [UnitTest(des:  "初始化")]
        static public void Register()
        {
            BResources.Load(AssetLoadPath.Editor);
        }
        
        
        
        [UnitTest(des: "加载测试")]
        static public void Load()
        {
            //同个目录
            var o = BResources.Load<GameObject>("AssetTest/Cube");
            o = BResources.Load<GameObject>("AssetTest/Particle");
            //不同的runtime目录
            o = BResources.Load<GameObject>("CubeSVN");
            o = BResources.Load<GameObject>("null");
            
        }
        
                
        [UnitTest(des: "加载测试2")]
        static public void LoadALL()
        {
            //同个目录
            var objs = BResources.LoadALL<Sprite>("LoadAllTest/timg");
            Assert.Equals(objs.Length, 4);
            
        }
        
        
        [UnitTest(des: "异步测试")]
        static public void AsyncLoad()
        {
        }
        
        [UnitTest(des: "批量加载测试")]
        static public void MultipleLoad()
        {
        }

        
        [UnitTest(des: "路径获取测试[单个]")]
        static public void GetAsset()
        {
            //寻找具体字符串
            var rets2 = BResources.ResLoader.GetAssets("AssetTest","Cu");
            Assert.Equals(rets2.Length, 1);
            Assert.Equals(rets2[0].ToLower(), "AssetTest/Cube".ToLower(),"资源获取出错");
        }

        
        [UnitTest(des:  "路径获取测试[批量]")]
        static public void GetFolderAssets()
        {
            //寻找目录下整个
            var rets = BResources.ResLoader.GetAssets("AssetTest");
            Debug.Log(JsonMapper.ToJson(rets));
            Assert.Equals(rets.Length, 6);
        }

    }
}