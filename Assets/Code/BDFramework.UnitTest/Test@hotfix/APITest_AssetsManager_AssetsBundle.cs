using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.UnitTest;
using Game.Data;
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
            //BResources.Load(Application.streamingAssetsPath);
        }

        
        
    }
}