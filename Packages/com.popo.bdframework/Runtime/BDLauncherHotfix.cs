using System;
using System.Diagnostics;
using System.Reflection;
using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using UnityEngine;


namespace BDFramework
{
    /// <summary>
    /// 运行时第二阶段启动协调器。
    /// 当 <c>BDLauncher</c> 完成程序集装载后，业务侧需要显式调用这里来串起配置加载、资源初始化、SQLite 初始化和管理器启动。
    /// </summary>
    /// <remarks>
    /// 典型用法是在更新页收到 <c>AssetsVersionController.RetStatus.Success</c> 后调用
    /// <c>BDLauncherHotfix.Launch()</c>，而不是在 <c>BDLauncher.Start()</c> 里直接进入业务流程。
    /// </remarks>
    public class BDLauncherBridge
    {
        private static readonly string Tag = "Launch";


        #region 启动热更逻辑

        /// <summary>
        /// 启动框架主体，包括配置、资源、SQLite 和热更管理器系统。
        /// </summary>
        /// <param name="gameId">保留给多游戏启动场景的兼容参数；当前默认流程未直接使用该值。</param>
        static public void Launch(string gameId = "default")
        {
            // Phase 1: 标记框架进入运行时阶段，并准备协程执行工具。
            BApplication.IsPlaying = true;
            BDLauncher.Inst.gameObject.AddComponent<IEnumeratorTool>();

            // Phase 2: 先加载框架基础配置，解析后续资源与数据库初始化所需的路径和版本号。
            GameConfigLoder.LoadFrameworkConfig();
            var Config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();

            BDebug.EnableLog(Tag);
            var clientVersion = Config.ClientVersionNum;
            BDebug.Log("框架版本:" + BDLauncher.FrameworkVersion, Color.cyan);
            BDebug.Log("母包版本:" + clientVersion, Color.cyan);

            // Phase 3: 先完成资源双路径解析与母包基础资源修复，确保资源系统和数据库有稳定输入。
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
            BDebug.Log(Tag, "----------资源版本验证.end----------", Color.yellow);
          
            // Phase 4: 依次启动资源、SQLite 与热更管理器，真正进入框架业务运行态。
            BResources.Init(Config.ArtRoot, firstLoadDir, secondLoadDir);
            SqliteLoder.Init(Config.SQLRoot, firstLoadDir, secondLoadDir);
            ScriptLoder.Start();

            // Phase 5: 尝试通过反射启动 E2E 测试系统。
            // 使用反射避免直接依赖 Talos.E2E 程序集，
            // 与 QuitFramework() 中的反射模式保持一致。
            // 当 Talos.E2E 包不存在或非 Debug 构建时，此调用安全无副作用。
            TryStartE2EFramework();
        }

        #endregion

        #region E2E 测试自动集成

        /// <summary>
        /// 通过反射尝试调用 Talos.E2E.E2EAutoInit.CheckAndLaunch()。
        /// 在热更 DLL 加载 + 框架初始化完成后调用，确保 E2E 测试可以正确发现热更类型。
        /// 如果 Talos.E2E 包不存在，或非 Debug 构建且无 -talosForceE2E 参数，则静默跳过。
        /// 只有 debug 模式生效，避免发布版本带上该功能！！！！
        /// Editor 和真机统一走此入口：
        /// - 真机：E2EAutoInit → LaunchE2E（MonoBehaviour 模式）
        /// - Editor PlayMode：E2EAutoInit → LaunchE2EStatic（静态模式，由 DidReloadScripts 管理）
        /// - Editor 非进 PlayMode：由 LaunchE2EEditorOnly 直接启动静态 TCP，不经此路径
        /// </summary>
        [Conditional("DEBUG")]
        static private void TryStartE2EFramework()
        {
            try
            {
                // 查找 Talos.E2E 程序集中的 E2EAutoInit 类型
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("Talos.E2E.E2EAutoInit");
                    if (type == null) continue;

                    var method = type.GetMethod("CheckAndLaunch",
                        BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                    {
                        UnityEngine.Debug.LogWarning("[BDLauncherBridge] 找到 E2EAutoInit 但无 CheckAndLaunch 方法");
                        return;
                    }

                    method.Invoke(null, new object[] { 10002 });
                    UnityEngine.Debug.Log("[BDLauncherBridge] E2E 测试自动检测已触发");
                    return;
                }

                // Talos.E2E 包未安装，正常跳过
            }
            catch (System.Exception ex)
            {
                // E2E 自动检测失败不影响框架正常启动
                UnityEngine.Debug.LogWarning($"[BDLauncherBridge] E2E 自动检测失败（不影响启动）: {ex.Message}");
            }
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 在 Editor 退出阶段释放 SQLite 和热更程序集相关资源。
        /// </summary>
            public  void OnApplicationQuit()
        {
#if UNITY_EDITOR
            SqliteLoder.Close();
            ScriptLoder.Dispose();
#endif
        }

        #endregion
    }

    /// <summary>
    /// 兼容旧工程仍通过 <c>BDLauncherHotfix</c> 访问启动与退出逻辑。
    /// 该类只做转发，不改变当前 <c>BDLauncherBridge</c> 的业务语义和执行顺序。
    /// </summary>
    public class BDLauncherHotfix
    {
        /// <summary>
        /// 兼容旧入口名的框架启动方法。
        /// </summary>
        /// <param name="gameId">保留给旧工程的兼容参数，会原样转发给 Bridge。</param>
        public static void Launch(string gameId = "default")
        {
            BDLauncherBridge.Launch(gameId);
        }

        /// <summary>
        /// 兼容旧入口名的退出收尾方法。
        /// </summary>
        public void OnApplicationQuit()
        {
            new BDLauncherBridge().OnApplicationQuit();
        }
    }
}