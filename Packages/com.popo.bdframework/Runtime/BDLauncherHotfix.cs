using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using UnityEngine;


namespace BDFramework
{
    /// <summary>
    /// BDLauncherBridge 负责热更逻辑的启动
    /// </summary>
    public class BDLauncherHotfix
    {
        private static readonly string Tag = "Launch";


        #region 启动热更逻辑

        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="launchSuccessCallback"></param>
        /// <param name="mainProjectTypes">Editor模式下,UPM隔离了DLL需要手动传入</param>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        static public void Launch(string gameId = "default")
        {
            BApplication.IsPlaying = true;
            BDLauncher.Inst.gameObject.AddComponent<IEnumeratorTool>();
            GameConfigLoder.LoadFrameBaseConfig();
            var Config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            //
            BDebug.EnableLog(Tag);
            var clientVersion = Config.ClientVersionNum;
            BDebug.Log("框架版本:" + BDLauncher.FrameworkVersion, Color.cyan);
            BDebug.Log("母包版本:" + clientVersion, Color.cyan);
            //开始资源检测
            BDebug.Log(Tag, "----------资源版本验证----------", Color.yellow);
            var (firstLoadDir, secondLoadDir) =
                ClientAssetsUtils.GetMultiAssetsLoadPath(BApplication.RuntimePlatform, clientVersion);
            if (Application.isEditor)
            {
                firstLoadDir = secondLoadDir;
            }

            BDebug.Log(Tag, "第一寻址路径: " + firstLoadDir, Color.magenta);
            BDebug.Log(Tag, "第二寻址路径: " + secondLoadDir, Color.magenta);
            ClientAssetsUtils.CheckBaseClientAssets(firstLoadDir, secondLoadDir);
            //平台
            BDebug.Log(Tag, "----------资源版本验证.end----------", Color.yellow);
          
            //1.美术资产初始化
            BResources.Init(Config.ArtRoot, firstLoadDir, secondLoadDir);
            //2.sql初始化
            SqliteLoder.Init(Config.SQLRoot, firstLoadDir, secondLoadDir);
            //3.脚本,这个启动会开启所有的逻辑
            HotfixScriptLoder.Start();
        }

        #endregion

        #region 生命周期

      public  void OnApplicationQuit()
        {
#if UNITY_EDITOR
            SqliteLoder.Close();
            HotfixAssembliesHelper.Dispose();
#endif
        }

        #endregion
    }
}