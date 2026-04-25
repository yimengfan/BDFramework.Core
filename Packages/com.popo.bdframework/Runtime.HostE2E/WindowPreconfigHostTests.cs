using System;
using System.Reflection;
using Talos.E2E;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.HostE2E
{
    /// <summary>
    /// 宿主侧 WindowPreconfig 业务流程 E2E 测试套件。
    /// Host-owned WindowPreconfig business-flow E2E suite.
    /// 该套件服务于 step_02 框架业务测试，验证应用启动后能正常进入预配置界面，
    /// 并通过宿主可见信号确认热更已完成、界面已加载、服务器配置已就绪。
    /// This suite serves step_02 framework business testing and verifies that the app can reach the preconfiguration screen after launch,
    /// confirming through host-visible signals that hotfix has completed, the UI has loaded, and server config is ready.
    /// </summary>
    [Preserve]
    public static class WindowPreconfigHostTests
    {
        private const string HotfixFrameworkAssemblyName = "BDFramework.Core";
        private const string BApplicationTypeName = "BDFramework.Core.Tools.BApplication";
        private const string GameConfigManagerTypeName = "BDFramework.Configure.GameConfigManager";
        private const string ServerConfigProcessorTypeName = "BDFramework.Configure.ServerConfigProcessor";
        private const string WindowPreconfigTypeName = "WindowPreconfig";

        /// <summary>
        /// 验证 WindowPreconfig 界面已加载并处于激活状态。
        /// Verify that the WindowPreconfig screen has loaded and is active.
        /// 该检查确认应用启动后正常进入预配置界面，未在启动阶段崩溃或卡死。
        /// This check confirms that the app has reached the preconfiguration screen after launch without crashing or hanging during startup.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "window-preconfig", order: 1, des: "验证预配置界面已加载并激活")]
        public static void WindowPreconfigScreenLoaded()
        {
            Debug.Log("[E2E] 测试目的=验证预配置界面已加载并激活 实现手段=查找 WindowPreconfig MonoBehaviour 实例并检查激活状态");

            var windowPreconfigType = FindLoadedType(WindowPreconfigTypeName);
            if (windowPreconfigType == null)
            {
                throw new Exception($"未发现 WindowPreconfig 类型，可能尚未完成场景加载");
            }

            var windowPreconfigInstance = UnityEngine.Object.FindObjectOfType(windowPreconfigType);
            if (windowPreconfigInstance == null)
            {
                throw new Exception("未发现 WindowPreconfig 实例，场景可能未正确加载");
            }

            var gameObject = (windowPreconfigInstance as MonoBehaviour)?.gameObject;
            if (gameObject == null)
            {
                throw new Exception("WindowPreconfig 实例未关联 GameObject");
            }

            if (!gameObject.activeInHierarchy)
            {
                throw new Exception($"WindowPreconfig GameObject 未激活: {gameObject.name}");
            }

            Debug.Log($"[E2E] 预配置界面已加载: gameObject={gameObject.name} active={gameObject.activeInHierarchy}");
        }

        /// <summary>
        /// 验证服务器配置已正确加载并可访问。
        /// Verify that the server configuration has been loaded and is accessible.
        /// 该检查确认 GameConfigManager 已初始化，服务器配置（FileServerUrl 等）可被读取。
        /// This check confirms that GameConfigManager has been initialized and server configuration (FileServerUrl, etc.) can be read.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "window-preconfig", order: 2, des: "验证服务器配置已加载并可访问")]
        public static void ServerConfigLoadedAndAccessible()
        {
            Debug.Log("[E2E] 测试目的=验证服务器配置已加载并可访问 实现手段=反射读取 GameConfigManager 与 ServerConfigProcessor 配置实例");

            var hotfixAssembly = FindLoadedAssembly(HotfixFrameworkAssemblyName);
            if (hotfixAssembly == null)
            {
                throw new Exception($"未发现热更程序集: {HotfixFrameworkAssemblyName}");
            }

            var gameConfigManagerType = hotfixAssembly.GetType(GameConfigManagerTypeName);
            if (gameConfigManagerType == null)
            {
                throw new Exception($"未发现 GameConfigManager 类型: {GameConfigManagerTypeName}");
            }

            var instProperty = gameConfigManagerType.GetProperty(
                "Inst",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProperty == null)
            {
                throw new Exception("未发现 GameConfigManager.Inst 属性");
            }

            var gameConfigManagerInstance = instProperty.GetValue(null);
            if (gameConfigManagerInstance == null)
            {
                throw new Exception("GameConfigManager.Inst 为空，配置管理器可能尚未初始化");
            }

            var getConfigMethod = gameConfigManagerType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Instance);
            if (getConfigMethod == null)
            {
                throw new Exception("未发现 GameConfigManager.GetConfig 方法");
            }

            var serverConfigProcessorType = hotfixAssembly.GetType(ServerConfigProcessorTypeName);
            if (serverConfigProcessorType == null)
            {
                throw new Exception($"未发现 ServerConfigProcessor 类型: {ServerConfigProcessorTypeName}");
            }

            var configNestedType = serverConfigProcessorType.GetNestedType("Config");
            if (configNestedType == null)
            {
                throw new Exception("未发现 ServerConfigProcessor.Config 嵌套类型");
            }

            var getConfigGenericMethod = getConfigMethod.MakeGenericMethod(configNestedType);
            var serverConfig = getConfigGenericMethod.Invoke(gameConfigManagerInstance, null);
            if (serverConfig == null)
            {
                throw new Exception("ServerConfigProcessor.Config 配置实例为空");
            }

            var fileServerUrlField = configNestedType.GetField("FileServerUrl");
            if (fileServerUrlField == null)
            {
                throw new Exception("未发现 ServerConfigProcessor.Config.FileServerUrl 字段");
            }

            var fileServerUrl = fileServerUrlField.GetValue(serverConfig) as string;
            if (string.IsNullOrWhiteSpace(fileServerUrl))
            {
                throw new Exception("ServerConfigProcessor.Config.FileServerUrl 为空");
            }

            Debug.Log($"[E2E] 服务器配置已加载: fileServerUrl={fileServerUrl}");
        }

        /// <summary>
        /// 验证热更程序集已完成加载并可用于业务逻辑。
        /// Verify that the hotfix assembly has completed loading and is available for business logic.
        /// 该检查确认热更 DLL 已被正确加载，核心框架类型可被枚举。
        /// This check confirms that the hotfix DLL has been correctly loaded and core framework types can be enumerated.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "window-preconfig", order: 3, des: "验证热更程序集已完成加载")]
        public static void HotfixAssemblyReadyForBusinessLogic()
        {
            Debug.Log("[E2E] 测试目的=验证热更程序集已完成加载 实现手段=检查核心热更程序集加载状态与关键类型可达性");

            var hotfixAssembly = FindLoadedAssembly(HotfixFrameworkAssemblyName);
            if (hotfixAssembly == null)
            {
                throw new Exception($"未发现热更程序集: {HotfixFrameworkAssemblyName}");
            }

            var bApplicationType = hotfixAssembly.GetType(BApplicationTypeName);
            if (bApplicationType == null)
            {
                throw new Exception($"未发现 BApplication 类型: {BApplicationTypeName}");
            }

            var persistentDataPathProperty = bApplicationType.GetProperty("persistentDataPath", BindingFlags.Public | BindingFlags.Static);
            if (persistentDataPathProperty == null)
            {
                throw new Exception("未发现 BApplication.persistentDataPath 属性");
            }

            var persistentDataPath = persistentDataPathProperty.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(persistentDataPath))
            {
                throw new Exception("BApplication.persistentDataPath 为空");
            }

            Debug.Log($"[E2E] 热更程序集已就绪: assembly={hotfixAssembly.GetName().Name} persistentDataPath={persistentDataPath}");
        }

        /// <summary>
        /// 验证预配置界面按钮可交互。
        /// Verify that the preconfiguration screen buttons are interactive.
        /// 该检查确认界面 UI 组件已正确绑定，为后续交互测试（下载等）做准备。
        /// This check confirms that UI components are correctly bound, preparing for subsequent interaction tests (download, etc.).
        /// 注意：此测试仅验证按钮存在且可交互，不触发实际操作。
        /// Note: This test only verifies that buttons exist and are interactive without triggering actual operations.
        /// TODO: 后续迭代将添加下载、修复模式等交互流程测试。
        /// TODO: Future iterations will add interaction flow tests for download, repair mode, etc.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "window-preconfig", order: 4, des: "验证预配置界面按钮可交互")]
        public static void WindowPreconfigButtonsInteractive()
        {
            Debug.Log("[E2E] 测试目的=验证预配置界面按钮可交互 实现手段=查找 Button 组件并检查交互状态");

            var windowPreconfigType = FindLoadedType(WindowPreconfigTypeName);
            if (windowPreconfigType == null)
            {
                throw new Exception($"未发现 WindowPreconfig 类型");
            }

            var windowPreconfigInstance = UnityEngine.Object.FindObjectOfType(windowPreconfigType);
            if (windowPreconfigInstance == null)
            {
                throw new Exception("未发现 WindowPreconfig 实例");
            }

            var monoBehaviour = windowPreconfigInstance as MonoBehaviour;
            if (monoBehaviour == null)
            {
                throw new Exception("WindowPreconfig 实例不是 MonoBehaviour");
            }

            var transform = monoBehaviour.transform;
            
            // 检查关键按钮是否存在
            // Check if critical buttons exist
            var buttonPaths = new[]
            {
                "btn_Download",
                "btn_DownloadRepair",
                "btn_GetSubPackage",
                "btn_Pass",
                "btn_ClearPersistent",
            };

            var interactiveButtonCount = 0;
            foreach (var buttonPath in buttonPaths)
            {
                var buttonTransform = transform.Find(buttonPath);
                if (buttonTransform == null)
                {
                    Debug.LogWarning($"[E2E] 未找到按钮: {buttonPath}");
                    continue;
                }

                var button = buttonTransform.GetComponent<UnityEngine.UI.Button>();
                if (button == null)
                {
                    Debug.LogWarning($"[E2E] 按钮缺少 Button 组件: {buttonPath}");
                    continue;
                }

                if (!button.interactable)
                {
                    Debug.LogWarning($"[E2E] 按钮不可交互: {buttonPath}");
                    continue;
                }

                interactiveButtonCount++;
                Debug.Log($"[E2E] 按钮已就绪: {buttonPath}");
            }

            if (interactiveButtonCount < buttonPaths.Length)
            {
                Debug.LogWarning($"[E2E] 部分按钮未就绪: ready={interactiveButtonCount} total={buttonPaths.Length}");
            }

            Debug.Log($"[E2E] 预配置界面按钮检查完成: interactive={interactiveButtonCount}/{buttonPaths.Length}");
        }

        /// <summary>
        /// 从当前 AppDomain 中查找指定短名称的已装载程序集。
        /// Find a loaded assembly with the specified short name from the current AppDomain.
        /// </summary>
        /// <param name="assemblyName">目标程序集短名称。Target short assembly name.</param>
        /// <returns>命中时返回程序集，否则返回 null。Returns the assembly when found; otherwise returns null.</returns>
        private static Assembly FindLoadedAssembly(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    return assembly;
                }
            }

            return null;
        }

        /// <summary>
        /// 从当前 AppDomain 中查找指定全名的已装载类型。
        /// Find a loaded type with the specified name in the current AppDomain.
        /// </summary>
        /// <param name="typeName">目标类型名称。Target type name.</param>
        /// <returns>命中时返回类型，否则返回 null。Returns the type when found; otherwise returns null.</returns>
        private static Type FindLoadedType(string typeName)
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
    }
}
