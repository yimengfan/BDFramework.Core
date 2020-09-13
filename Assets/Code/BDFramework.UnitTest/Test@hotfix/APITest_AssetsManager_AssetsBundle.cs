using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.UnitTest;
using Game.Data;
using LitJson;
using UnityEngine;

namespace Code.BDFramework.UnitTest
{
    [UnitTestAttribute(Des = "资源管理测试-Assetbundle")]
    static public class APITest_AssetsManager_AssetsBundle
    {
        
                
        [UnitTest(Des = "初始化")]
        static public void Register()
        {
            //这里开启Assetbundle模式
            BResources.Load(Application.streamingAssetsPath);
        }
        
        [UnitTest(Des = "关闭",Order = 10000)]
        static public void Close()
        {
            BResources.UnloadAll();
        }

        [UnitTest(Des = "加载测试")]
        static public void Load()
        {
            APITest_AssetsManager_DevResource.Load();
        }
        
                
        [UnitTest(Des = "加载测试2")]
        static public void LoadALL()
        {
            APITest_AssetsManager_DevResource.LoadALL();
            
        }
        
        
        [UnitTest(Des = "异步测试")]
        static public void AsyncLoad()
        {
            APITest_AssetsManager_DevResource.AsyncLoad();
        }
        
        [UnitTest(Des = "批量加载测试")]
        static public void MultipleLoad()
        {
            APITest_AssetsManager_DevResource.MultipleLoad();
        }

                
        [UnitTest(Des = "路径获取测试【单个】")]
        static public void GetAsset()
        {
            APITest_AssetsManager_DevResource.GetAsset();
        }

        
        [UnitTest(Des = "路径获取测试【批量】")]
        static public void GetFolderAssets()
        {
            APITest_AssetsManager_DevResource.GetFolderAssets();
        }
        
        
    }
}