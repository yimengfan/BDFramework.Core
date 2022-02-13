using BDFramework;
using BDFramework.ResourceMgr;
using BDFramework.UnitTest;

namespace BDFramework.UnitTest
{
    [UnitTest(des:  "资源管理测试-Assetbundle")]
    static public class APITest_AssetsManager_AssetsBundle
    {
        
                
        [UnitTest(des: "初始化")]
        static public void Register()
        {
            //这里开启Assetbundle模式
            BResources.Load(AssetLoadPathType.DevOpsPublish);
        }
        
        [UnitTest(10000,"关闭")]
        static public void Close()
        {
            BResources.UnloadAll();
        }

        [UnitTest(des: "加载测试")]
        static public void Load()
        {
            APITest_AssetsManager_DevResource.Load();
        }
        [UnitTest(des: "加载测试-同名")]
        static public void LoadSameNameAsset()
        {
            APITest_AssetsManager_DevResource.LoadSameNameAsset();
        }
                
        [UnitTest(des:  "加载测试所有")]
        static public void LoadALL()
        {
            APITest_AssetsManager_DevResource.LoadALL();
            
        }
        
        
        [UnitTest(des:  "异步测试")]
        static public void AsyncLoad()
        {
            APITest_AssetsManager_DevResource.AsyncLoad();
        }
        
        [UnitTest(des:  "批量加载测试")]
        static public void MultipleLoad()
        {
            APITest_AssetsManager_DevResource.MultipleLoad();
        }

                
        [UnitTest(des:  "路径获取测试【单个】")]
        static public void GetAsset()
        {
            APITest_AssetsManager_DevResource.GetAsset();
        }

        
        [UnitTest(des:  "路径获取测试【批量】")]
        static public void GetFolderAssets()
        {
            APITest_AssetsManager_DevResource.GetFolderAssets();
        }
        
        
    }
}