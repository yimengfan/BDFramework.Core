using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.HostE2E
{
    /// <summary>
    /// AOT 预配置界面。
    /// AOT preconfiguration screen.
    /// 该界面在资源下载与进入主流程前保留宿主侧可见入口，并在 Talos E2E 强制模式下由宿主显式桥接测试启动。
    /// This screen keeps the host-owned visible entry before resource download and main-flow launch, and explicitly bridges Talos E2E startup from host code in forced mode.
    /// 注意：该类使用反射访问热更程序集类型，避免 AOT 对热更类型的直接引用。
    /// Note: This class uses reflection to access hotfix assembly types, avoiding direct AOT-to-hotfix type references.
    /// </summary>
    public class WindowPreconfigAOT : MonoBehaviour
    {
        private InputField inputField;
        private Text text_DownloadProcess;
        private Button btn_Download;
        private Button btn_DownloadRepair;
        private Button btn_GetSubPackage;
        private Button btn_Pass;
        private Button btn_ClearPersistent;

        /// <summary>
        /// 缓存的服务器配置 FileServerUrl。
        /// Cached FileServerUrl from server configuration.
        /// </summary>
        private string cachedFileServerUrl;

        /// <summary>
        /// 热更程序集名称常量。
        /// Hotfix assembly name constant.
        /// </summary>
        private const string HotfixAssemblyName = "Assembly-CSharp";

        /// <summary>
        /// 预配置界面启动入口。
        /// Startup entry for the preconfiguration screen.
        /// 负责绑定 UI、加载服务器配置，并在 `-talosForceE2E` 模式下显式根引用宿主侧 launch/BaseFlow 测试入口后再触发 Talos E2E 自动启动入口。
        /// It binds UI, loads server configuration, explicitly roots the host-owned launch/BaseFlow test entrypoints under `-talosForceE2E`, and then triggers the Talos E2E auto-start entry.
        /// </summary>
        void Start()
        {
            // 节点发现
            // Node discovery
            inputField = this.transform.Find("InputField").GetComponent<InputField>();
            text_DownloadProcess = this.transform.Find("text_DownloadProcess").GetComponent<Text>();
            btn_Download = this.transform.Find("btn_Download").GetComponent<Button>();
            btn_DownloadRepair = this.transform.Find("btn_DownloadRepair").GetComponent<Button>();
            btn_GetSubPackage = this.transform.Find("btn_GetSubPackage").GetComponent<Button>();
            btn_Pass = this.transform.Find("btn_Pass").GetComponent<Button>();
            btn_ClearPersistent = this.transform.Find("btn_ClearPersistent").GetComponent<Button>();

            this.btn_Pass.onClick.AddListener(Onclick_PassAndLaunch);
            this.btn_Download.onClick.AddListener(OnClick_DownLoadAndLaunch);
            this.btn_DownloadRepair.onClick.AddListener(Onclick_Download_RepairMode);
            this.btn_GetSubPackage.onClick.AddListener(OnClick_GetSubPackage);
            this.btn_ClearPersistent.onClick.AddListener(OnClick_ClearPersistent);

            // 使用反射获取服务器配置
            // Use reflection to get server configuration
            this.cachedFileServerUrl = GetServerConfigFileServerUrl();
            if (Application.isEditor)
            {
                inputField.text = this.cachedFileServerUrl;
            }
            else
            {
                inputField.text = this.cachedFileServerUrl;
            }
            Debug.Log("FileServer:" + this.cachedFileServerUrl);

            var commandLineArgs = RuntimeLaunchArguments.ResolveCurrentProcessArguments();
            if (ForcedModeStartupFallback.TryLaunchFromForcedMode(commandLineArgs, 10002, E2EAutoInit.CheckAndLaunch))
            {
                // 显式根引用宿主侧 launch 与 BaseFlow 套件类型，避免 Player 端只保留 launch 类型而裁剪新增的基础系统入口。
                // Explicitly root the host launch and BaseFlow suite types so player builds do not keep only the launch type while stripping the added foundational entrypoints.
                var hostLaunchSuiteAssembly = typeof(LaunchFlowHostTests).Assembly;
                var hostBaseFlowSuiteAssembly = typeof(BaseFlowHostRuntimeTests).Assembly;
                Debug.Log(
                    $"[TalosE2E] 宿主已绑定 launch/BaseFlow 宿主测试程序集: launch={hostLaunchSuiteAssembly.GetName().Name} baseflow={hostBaseFlowSuiteAssembly.GetName().Name}");
                Debug.Log("[TalosE2E] 当前处于 -talosForceE2E 模式，WindowPreconfigAOT 已显式补触发 E2E 自动检测；若 ScriptLoder.Init 已启动，该调用会被安全忽略");
            }
        }

        /// <summary>
        /// 点击按钮事件。
        /// Button click event.
        /// </summary>
        void Onclick_PassAndLaunch()
        {
            // 使用反射启动热更逻辑
            // Use reflection to launch hotfix logic
            LaunchHotfix();
            this.StartCoroutine(IE_Destroy());
        }

        /// <summary>
        /// 下载且启动。
        /// Download and launch.
        /// </summary>
        private void OnClick_DownLoadAndLaunch()
        {
            Debug.Log(GetPersistentDataPath());
            var url = cachedFileServerUrl;
            float totalSize = -1;
            float curDownloadSize = -1;

            StartAssetsVersionControl(UpdateMode.CompareWithRepairCoreAssets, url, null,
                (curDownload, allDownloadList) =>
                {
                    if (totalSize == -1)
                    {
                        foreach (var item in allDownloadList)
                        {
                            totalSize += item.FileSize;
                        }
                        curDownloadSize = 0;
                    }
                    curDownloadSize += curDownload.FileSize;
                    // 进度通知，显示下载的
                    // Progress notification, display downloaded
                    this.text_DownloadProcess.text = string.Format("{0}KB / {1}KB", curDownloadSize, totalSize);
                },
                (status, msg) =>
                {
                    // 下载状态
                    // Download status
                    switch (status)
                    {
                        case AssetsVersionController.RetStatus.Success:
                        {
                            this.text_DownloadProcess.text = "下载完毕";
                        }
                            break;
                        case AssetsVersionController.RetStatus.SuccessNeedRestart:
                        {
                            this.text_DownloadProcess.text = "下载完毕，有代码更新，请重启游戏";
                        }
                            break;
                        case AssetsVersionController.RetStatus.Error:
                        {
                            this.text_DownloadProcess.text = msg;
                        }
                            break;
                    }
                });
        }

        /// <summary>
        /// 下载资源 - 修复模式。
        /// Download resources - repair mode.
        /// </summary>
        private void Onclick_Download_RepairMode()
        {
            Debug.Log(GetPersistentDataPath());
            var url = cachedFileServerUrl;
            float totalSize = 0;
            float curDownloadSize = 0;
            StartAssetsVersionControl(UpdateMode.RepairFull, url, null, (curDownload, allDownloadList) =>
                {
                    if (totalSize == 0)
                    {
                        foreach (var item in allDownloadList)
                        {
                            totalSize += item.FileSize;
                        }
                        curDownloadSize = 0;
                    }
                    curDownloadSize += curDownload.FileSize;
                    // 进度通知，显示下载的
                    // Progress notification, display downloaded
                    this.text_DownloadProcess.text = string.Format("{0}KB / {1}KB", curDownloadSize, totalSize);
                },
                (status, msg) =>
                {
                    // 下载状态
                    // Download status
                    switch (status)
                    {
                        case AssetsVersionController.RetStatus.Success:
                        {
                            this.text_DownloadProcess.text = "下载完毕";
                        }
                            break;
                        case AssetsVersionController.RetStatus.SuccessNeedRestart:
                        {
                            this.text_DownloadProcess.text = "下载完毕，请重启游戏";
                        }
                            break;
                        case AssetsVersionController.RetStatus.Error:
                        {
                            this.text_DownloadProcess.text = msg;
                        }
                            break;
                    }
                });
        }

        /// <summary>
        /// 获取子包。
        /// Get subpackages.
        /// </summary>
        private void OnClick_GetSubPackage()
        {
            var url = cachedFileServerUrl;
            GetServerSubPacks(url, (map) =>
            {
                // 获取到子包
                // Subpackages received
                Debug.Log("获取到子包信息:\n" + LitJson.JsonMapper.ToJson(map, true));

                // 全隐藏
                // Hide all
                var grid = this.transform.Find("grid_SubPack");
                foreach (Transform child in grid)
                {
                    child.gameObject.SetActive(false);
                }

                // 显示
                // Show
                var idx = 0;
                foreach (var kv in map)
                {
                    var btn = grid.GetChild(idx)?.GetComponent<Button>();
                    btn.gameObject.SetActive(true);
                    btn.onClick.RemoveAllListeners();
                    btn.transform.GetChild(0).GetComponent<Text>().text = "下载子包:" + kv.Key;
                    // 添加监听
                    // Add listener
                    btn.onClick.AddListener(() =>
                    {
                        // 下载
                        // Download
                        this.Onclick_DownloadSubPackageLoadAndLaunch(kv.Key);
                    });
                    idx++;
                }
            });
        }

        /// <summary>
        /// 下载分包且启动。
        /// Download subpackage and launch.
        /// </summary>
        private void Onclick_DownloadSubPackageLoadAndLaunch(string subPackageName)
        {
            Debug.Log(Application.persistentDataPath);

            var url = cachedFileServerUrl;
            float totalSize = -1;
            float curDownloadSize = -1;
            StartAssetsVersionControl(UpdateMode.CompareSimple, url, subPackageName,
                (curDownload, allDownloadList) =>
                {
                    if (totalSize == -1)
                    {
                        foreach (var item in allDownloadList)
                        {
                            totalSize += item.FileSize;
                        }
                        curDownloadSize = 0;
                    }
                    curDownloadSize += curDownload.FileSize;
                    // 进度通知
                    // Progress notification
                    this.text_DownloadProcess.text = string.Format("{0}KB / {1}KB", curDownloadSize, totalSize);
                },
                (status, msg) =>
                {
                    switch (status)
                    {
                        case AssetsVersionController.RetStatus.Success:
                        {
                            this.text_DownloadProcess.text = "下载完毕";
                            Debug.Log("分包下载完毕,此时资源不全,进入游戏可能会有bug!");
                        }
                            break;
                        case AssetsVersionController.RetStatus.SuccessNeedRestart:
                        {
                            this.text_DownloadProcess.text = "下载完毕，请重启游戏";
                            Debug.Log("分包下载完毕，且包含 DLL 更新，请重启游戏后再进入。");
                        }
                            break;
                        case AssetsVersionController.RetStatus.Error:
                        {
                            this.text_DownloadProcess.text = msg;
                        }
                            break;
                    }
                });
        }

        /// <summary>
        /// 删除。
        /// Destroy.
        /// </summary>
        IEnumerator IE_Destroy()
        {
            yield return new WaitForSeconds(3);
            Destroy(this.gameObject);
        }

        /// <summary>
        /// 清理 persistent。
        /// Clear persistent storage.
        /// </summary>
        private void OnClick_ClearPersistent()
        {
            foreach (var runtime in GetSupportPlatform())
            {
                var path = Path.Combine(Application.persistentDataPath, GetPlatformLoadPath(runtime));
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            // 清理完毕
            // Cleanup complete
            var dirs = Directory.GetDirectories(Application.persistentDataPath, "*");
            Debug.Log(Application.persistentDataPath);
            Debug.Log("清理完毕~,剩余目录:" + dirs.Length);
        }

        #region 反射辅助方法 / Reflection Helper Methods

        /// <summary>
        /// 从热更程序集中获取已加载的类型。
        /// Get a loaded type from hotfix assemblies.
        /// </summary>
        /// <param name="typeName">类型全名。Full type name.</param>
        /// <returns>找到的类型，否则返回 null。Found type, or null.</returns>
        private static Type FindHotfixType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取服务器配置的 FileServerUrl。
        /// Get FileServerUrl from server configuration.
        /// 使用反射访问热更程序集中的 ServerConfigProcessor.Config 类型。
        /// Uses reflection to access ServerConfigProcessor.Config type in hotfix assembly.
        /// </summary>
        /// <returns>FileServerUrl 字符串，失败时返回空字符串。FileServerUrl string, or empty string on failure.</returns>
        private string GetServerConfigFileServerUrl()
        {
            try
            {
                // 查找 GameConfigManager 类型（在 BDFramework.Core 程序集中）
                // Find GameConfigManager type (in BDFramework.Core assembly)
                var gameConfigManagerType = FindHotfixType("BDFramework.Configure.GameConfigManager");
                if (gameConfigManagerType == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] 未找到 GameConfigManager 类型，使用默认配置");
                    return "127.0.0.1";
                }

                // 获取 Inst 属性
                // Get Inst property
                var instProperty = gameConfigManagerType.GetProperty("Inst", BindingFlags.Public | BindingFlags.Static);
                if (instProperty == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] 未找到 GameConfigManager.Inst 属性");
                    return "127.0.0.1";
                }

                var gameConfigManagerInstance = instProperty.GetValue(null);
                if (gameConfigManagerInstance == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] GameConfigManager.Inst 为空");
                    return "127.0.0.1";
                }

                // 获取 GetConfig 方法
                // Get GetConfig method
                var getConfigMethod = gameConfigManagerType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Instance);
                if (getConfigMethod == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] 未找到 GameConfigManager.GetConfig 方法");
                    return "127.0.0.1";
                }

                // 查找 ServerConfigProcessor.Config 类型（在热更程序集中）
                // Find ServerConfigProcessor.Config type (in hotfix assembly)
                var serverConfigProcessorType = FindHotfixType("Game.Config.ServerConfigProcessor");
                if (serverConfigProcessorType == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] 未找到 ServerConfigProcessor 类型，使用默认配置");
                    return "127.0.0.1";
                }

                var configNestedType = serverConfigProcessorType.GetNestedType("Config");
                if (configNestedType == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] 未找到 ServerConfigProcessor.Config 嵌套类型");
                    return "127.0.0.1";
                }

                // 调用 GetConfig<ServerConfigProcessor.Config>()
                // Call GetConfig<ServerConfigProcessor.Config>()
                var getConfigGenericMethod = getConfigMethod.MakeGenericMethod(configNestedType);
                var serverConfig = getConfigGenericMethod.Invoke(gameConfigManagerInstance, null);
                if (serverConfig == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] ServerConfigProcessor.Config 配置实例为空");
                    return "127.0.0.1";
                }

                // 获取 FileServerUrl 字段
                // Get FileServerUrl field
                var fileServerUrlField = configNestedType.GetField("FileServerUrl");
                if (fileServerUrlField == null)
                {
                    Debug.LogWarning("[WindowPreconfigAOT] 未找到 ServerConfigProcessor.Config.FileServerUrl 字段");
                    return "127.0.0.1";
                }

                var fileServerUrl = fileServerUrlField.GetValue(serverConfig) as string;
                if (string.IsNullOrWhiteSpace(fileServerUrl))
                {
                    Debug.LogWarning("[WindowPreconfigAOT] ServerConfigProcessor.Config.FileServerUrl 为空");
                    return "127.0.0.1";
                }

                return fileServerUrl;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WindowPreconfigAOT] 获取服务器配置失败: {ex.Message}");
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 使用反射启动热更逻辑。
        /// Launch hotfix logic using reflection.
        /// </summary>
        private void LaunchHotfix()
        {
            try
            {
                // 查找 BDLauncherHotfix 类型（在 BDFramework.Core 程序集中）
                // Find BDLauncherHotfix type (in BDFramework.Core assembly)
                var bdLauncherHotfixType = FindHotfixType("BDFramework.BDLauncherHotfix");
                if (bdLauncherHotfixType == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 BDLauncherHotfix 类型");
                    return;
                }

                // 获取 Launch 方法
                // Get Launch method
                var launchMethod = bdLauncherHotfixType.GetMethod("Launch", BindingFlags.Public | BindingFlags.Static);
                if (launchMethod == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 BDLauncherHotfix.Launch 方法");
                    return;
                }

                // 调用 Launch()
                // Call Launch()
                launchMethod.Invoke(null, new object[] { "default" });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowPreconfigAOT] 启动热更失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取 persistentDataPath（使用反射访问 BApplication）。
        /// Get persistentDataPath using reflection to access BApplication.
        /// </summary>
        private string GetPersistentDataPath()
        {
            try
            {
                var bApplicationType = FindHotfixType("BDFramework.Core.Tools.BApplication");
                if (bApplicationType == null)
                {
                    return Application.persistentDataPath;
                }

                var property = bApplicationType.GetProperty("persistentDataPath", BindingFlags.Public | BindingFlags.Static);
                if (property == null)
                {
                    return Application.persistentDataPath;
                }

                return property.GetValue(null) as string ?? Application.persistentDataPath;
            }
            catch
            {
                return Application.persistentDataPath;
            }
        }

        /// <summary>
        /// 获取支持的平台列表（使用反射访问 BApplication）。
        /// Get supported platforms using reflection to access BApplication.
        /// </summary>
        private RuntimePlatform[] GetSupportPlatform()
        {
            try
            {
                var bApplicationType = FindHotfixType("BDFramework.Core.Tools.BApplication");
                if (bApplicationType == null)
                {
                    return new[] { Application.platform };
                }

                var property = bApplicationType.GetProperty("SupportPlatform", BindingFlags.Public | BindingFlags.Static);
                if (property == null)
                {
                    return new[] { Application.platform };
                }

                return property.GetValue(null) as RuntimePlatform[] ?? new[] { Application.platform };
            }
            catch
            {
                return new[] { Application.platform };
            }
        }

        /// <summary>
        /// 获取平台加载路径（使用反射访问 BApplication）。
        /// Get platform load path using reflection to access BApplication.
        /// </summary>
        private string GetPlatformLoadPath(RuntimePlatform platform)
        {
            try
            {
                var bApplicationType = FindHotfixType("BDFramework.Core.Tools.BApplication");
                if (bApplicationType == null)
                {
                    return platform.ToString().ToLower();
                }

                var method = bApplicationType.GetMethod("GetPlatformLoadPath", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    return platform.ToString().ToLower();
                }

                return method.Invoke(null, new object[] { platform }) as string ?? platform.ToString().ToLower();
            }
            catch
            {
                return platform.ToString().ToLower();
            }
        }

        /// <summary>
        /// 启动资源版本控制（使用反射访问 BResources）。
        /// Start assets version control using reflection to access BResources.
        /// </summary>
        private void StartAssetsVersionControl(UpdateMode mode, string url, string subPackageName,
            Action<dynamic, dynamic> onProgress, Action<AssetsVersionController.RetStatus, string> onComplete)
        {
            try
            {
                var bresourcesType = FindHotfixType("BDFramework.BResources");
                if (bresourcesType == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 BResources 类型");
                    onComplete?.Invoke(AssetsVersionController.RetStatus.Error, "BResources 类型未找到");
                    return;
                }

                var method = bresourcesType.GetMethod("StartAssetsVersionControl", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 BResources.StartAssetsVersionControl 方法");
                    onComplete?.Invoke(AssetsVersionController.RetStatus.Error, "StartAssetsVersionControl 方法未找到");
                    return;
                }

                // 转换 UpdateMode 枚举值
                // Convert UpdateMode enum value
                var updateModeType = FindHotfixType("BDFramework.ResourceMgr.UpdateMode");
                if (updateModeType == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 UpdateMode 类型");
                    onComplete?.Invoke(AssetsVersionController.RetStatus.Error, "UpdateMode 类型未找到");
                    return;
                }

                var updateModeValue = Enum.ToObject(updateModeType, (int)mode);

                method.Invoke(null, new object[] { updateModeValue, url, subPackageName, onProgress, onComplete });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowPreconfigAOT] 启动资源版本控制失败: {ex.Message}");
                onComplete?.Invoke(AssetsVersionController.RetStatus.Error, ex.Message);
            }
        }

        /// <summary>
        /// 获取服务器子包（使用反射访问 BResources）。
        /// Get server subpackages using reflection to access BResources.
        /// </summary>
        private void GetServerSubPacks(string url, Action<dynamic> onComplete)
        {
            try
            {
                var bresourcesType = FindHotfixType("BDFramework.BResources");
                if (bresourcesType == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 BResources 类型");
                    return;
                }

                var method = bresourcesType.GetMethod("GetServerSubPacks", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError("[WindowPreconfigAOT] 未找到 BResources.GetServerSubPacks 方法");
                    return;
                }

                method.Invoke(null, new object[] { url, onComplete });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowPreconfigAOT] 获取服务器子包失败: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 资源版本控制器返回状态。
    /// Assets version controller return status.
    /// 注意：此枚举定义在 AOT 程序集中，与热更程序集中的定义对应。
    /// Note: This enum is defined in the AOT assembly, corresponding to the definition in the hotfix assembly.
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// 比较模式（带核心资源修复）。
        /// Compare mode (with core asset repair).
        /// </summary>
        CompareWithRepairCoreAssets = 0,

        /// <summary>
        /// 完整修复模式。
        /// Full repair mode.
        /// </summary>
        RepairFull = 1,

        /// <summary>
        /// 简单比较模式。
        /// Simple compare mode.
        /// </summary>
        CompareSimple = 2,
    }

    /// <summary>
    /// 资源版本控制器。
    /// Assets version controller.
    /// 注意：此类型定义在 AOT 程序集中，用于定义返回状态枚举。
    /// Note: This type is defined in the AOT assembly for defining return status enum.
    /// </summary>
    public static class AssetsVersionController
    {
        /// <summary>
        /// 返回状态。
        /// Return status.
        /// </summary>
        public enum RetStatus
        {
            /// <summary>
            /// 成功。
            /// Success.
            /// </summary>
            Success = 0,

            /// <summary>
            /// 成功，需要重启。
            /// Success, need restart.
            /// </summary>
            SuccessNeedRestart = 1,

            /// <summary>
            /// 错误。
            /// Error.
            /// </summary>
            Error = 2,
        }
    }
}
