using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Configure;
using BDFramework.Mgr;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixPipeline;
using BDFramework.Editor.Table;
using BDFramework.Editor.Task;
using BDFramework.Editor.Tools.EditorHttpServer;
using BDFramework.Hotfix.Reflection;
using BDFramework.ScreenView;
using ServiceStack;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Environment
{
    /// <summary>
    /// Editor下框架环境创建
    /// </summary>
    // [InitializeOnLoad]
    static public class BDFrameworkEditorEnvironment
    {
        /// <summary>
        /// 是否完成初始化
        /// </summary>
        static public bool IsInited { get; private set; } = false;

        /// <summary>
        /// 编辑器任务的
        /// </summary>
        static public EditorTask EditorTaskInstance { get; private set; } = null;

        /// <summary>
        /// Editor http任务
        /// </summary>
        static public EditorHttpListener EditorHttpListener { get; private set; }

        [InitializeOnLoadMethod]
        static void BDFrameworkEditorEnvironmentInit()
        {
            //TODO 
            //一般情况下 打开unity.或者reloadAssembly 会重新初始化框架
            //但是ExitPlaymode后不会触发ReloadAssembly,所以有些静态对象会缓存
            //非播放模式，初始化框架编辑器
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                InitEditorEnvironment();
            }

            //防止重复注册事件
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
            EditorApplication.update -= EditorUpdate_CheckGuideWindow;
            EditorApplication.update += EditorUpdate_CheckGuideWindow;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// 代码编译完成后
        /// </summary>
        // [UnityEditor.Callbacks.DidReloadScripts(0)]
        // static void OnScriptReload()
        // {
        //     OnCodeBuildComplete();
        // }

        /// <summary>
        /// 退出播放模式
        /// </summary>
        /// <param name="state"></param>
        static private void OnPlayModeChanged(PlayModeStateChange state)
        {
            //非播放模式,初始化框架~
            switch (state)
            {
                //-------------Editor mode--------------
                case PlayModeStateChange.EnteredEditMode:
                {
                    InitEditorEnvironment();
                }
                    break;
                case PlayModeStateChange.ExitingEditMode:
                {
                }
                    break;
                //-------------Play mode--------------
                case PlayModeStateChange.EnteredPlayMode:
                {
                    OnEnterPlayMode();
                }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                {
                    OnExitPlayMode();
                }
                    break;
            }
        }


        /// <summary>
        /// 初始化框架编辑器
        /// </summary>
        static public void InitEditorEnvironment()
        {
            //是否为batchmode
            if (Application.isBatchMode)
            {
                Debug.Log("BDFramework version:" + BDLauncher.FrameworkVersion);
            }

            //只有在非Playing的时候才初始化
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsInited)
            {
                return;
            }

            try
            {
                //BD初始化
                //BApplication.Init();
                //BDEditor初始化
                BDEditorApplication.Init();
                //加载主工程的DLL Type
                Types = ScriptLoder.GetAppDomainHostingTypes().ToArray();
                // var assemblyPath = BApplication.Library + "/ScriptAssemblies/Assembly-CSharp.dll";
                // var editorAssemlyPath = BApplication.Library + "/ScriptAssemblies/Assembly-CSharp-Editor.dll";
                // if (File.Exists(assemblyPath) && File.Exists(editorAssemlyPath))
                // {
                //     var gAssembly = Assembly.LoadFile(assemblyPath);
                //     var eAssemlby = Assembly.LoadFile(editorAssemlyPath);
                //     Types = CollectTypes(gAssembly, eAssemlby).ToArray();
                // }

                
                //编辑器下加载初始化
                BResources.Init(AssetLoadPathType.Editor);
                //编辑器下管理器注册
                ManagerInstHelper.LoadManager(Types);
                //加载框架配置
                GameConfigLoder.LoadFrameworkConfig(); 
                
                //Editor的管理器初始化
                BDFrameworkPipelineHelper.Init();
                //Pipeline初始化
                HotfixPipelineTools.Init();
                //编辑器初始化
                InitEditorTask();
                //编辑器任务
                EditorTaskInstance.OnUnityLoadOrCodeRecompiled();
                //编辑器http服务
                InitEditorHttpServer();
                //最后，完成初始化
                IsInited = true;
                //  Debug.Log("框架编辑器环境初始化成功!");
            }
            catch (Exception e)
            {
                Debug.LogError("框架编辑器环境初始化失败!");
                Debug.LogError(e.StackTrace);
                throw;
            }
        }


        #region 主工程 Assembly

        /// <summary>
        /// 游戏逻辑的Assembly
        /// </summary>
        static public Type[] Types { get; private set; } = new Type[] { };

        // /// <summary>
        // /// 外部注册主工程的Assembly
        // /// </summary>
        // /// <param name="gameLogicAssembly"></param>
        // /// <param name="gameEditorAssembly"></param>
        // static public List<Type> CollectTypes(Assembly gameLogicAssembly, Assembly gameEditorAssembly)
        // {
        //     //编辑器所有类
        //     List<Type> typeList = new List<Type>();
        //     typeList.AddRange(gameLogicAssembly.GetTypes());
        //     typeList.AddRange(gameEditorAssembly.GetTypes());
        //     //BD编辑器下所有的类
        //     typeList.AddRange(typeof(BDFrameworkEditorEnvironment).Assembly.GetTypes());
        //     //BDRuntime下所有类
        //     typeList.AddRange(typeof(BDLauncher).Assembly.GetTypes());
        //
        //     //
        //     return typeList;
        // }

        #endregion


        /// <summary>
        /// 编辑器的Update
        /// </summary>
        static public void EditorUpdate()
        {
            //编辑器任务的update
            EditorTaskInstance?.OnEditorUpdate();
        }


        /// <summary>
        /// 当进入paymode
        /// </summary>
        static private void OnEnterPlayMode()
        {
            BDEditorApplication.Init();
            InitEditorTask();
            EditorTaskInstance.OnEnterWillPlayMode();
        }

        /// <summary>
        /// 当进入paymode
        /// </summary>
        static private void OnExitPlayMode()
        {
        }

        /// <summary>
        /// 引导启动页面
        /// </summary>
        static public void EditorUpdate_CheckGuideWindow()
        {
            EditorApplication.update -= EditorUpdate_CheckGuideWindow;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorWindow_BDFrameworkStart.AutoOpen();
            }
        }

        /// <summary>
        /// 初始化editor task
        /// </summary>
        static private void InitEditorTask()
        {
            //编辑器任务执行
            if (EditorTaskInstance == null)
            {
                EditorTaskInstance = new EditorTask();
                EditorTaskInstance.CollectEditorTaskMedthod();
            }
        }

        /// <summary>
        /// 初始化 editor http
        /// </summary>
        static private void InitEditorHttpServer()
        {
            EditorHttpListener = new EditorHttpListener();
            EditorHttpListener.Start("+", "9999", "9998", "9997", "9996");
            EditorHttpListener.AddWebAPIProccesor<WP_EditorInvoke>();
        }
    }

    /// <summary>
    /// BatchMode 命令行参数读取工具。
    /// 该类型只负责从当前 Unity 进程命令行或显式参数列表中提取参数值，不负责解释具体业务含义，供母包、ClientRes 和验证 owner 复用。
    /// 示例：BuildTools_Assets 可通过 <c>GetArg("-ciOutputRoot")</c> 读取 CI 输出目录参数。
    /// </summary>
    static public class BatchModeCommandLine
    {
        /// <summary>
        /// 从当前 Unity 进程命令行中读取指定参数值。
        /// </summary>
        static public string GetArg(string argName)
        {
            return GetArg(System.Environment.GetCommandLineArgs(), argName);
        }

        /// <summary>
        /// 从当前 Unity 进程命令行中读取布尔参数。
        /// 约定：当参数缺失、值为空或值无法识别时，统一回退到调用方提供的默认值。
        /// </summary>
        static public bool GetBoolArg(string argName, bool defaultValue = false)
        {
            return GetBoolArg(System.Environment.GetCommandLineArgs(), argName, defaultValue);
        }

        /// <summary>
        /// 从显式参数列表中读取指定参数值。
        /// 该重载主要服务于测试或局部复用，避免调用方自己重复遍历参数数组。
        /// </summary>
        static internal string GetArg(IReadOnlyList<string> args, string argName)
        {
            if (args == null || string.IsNullOrWhiteSpace(argName))
            {
                return null;
            }

            for (var index = 0; index < args.Count - 1; index++)
            {
                if (string.Equals(args[index], argName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return null;
        }

        /// <summary>
        /// 从显式参数列表中读取布尔参数。
        /// 支持 true/false、1/0、yes/no、on/off，并忽略大小写与首尾空白。
        /// </summary>
        static internal bool GetBoolArg(IReadOnlyList<string> args, string argName, bool defaultValue = false)
        {
            var rawValue = GetArg(args, argName);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            switch (rawValue.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    return false;
                default:
                    return defaultValue;
            }
        }
    }

    /// <summary>
    /// BatchMode Android External Tools 自动补齐器。
    /// 该类型属于编辑器基础设施层，负责在 CI BatchMode Android 构建前探测并写回可用的 JDK / SDK / NDK，
    /// 避免具体构建 owner 重复维护同一套环境发现逻辑。
    /// 示例：BuildTools_ClientPackage 和 BuildTools_Assets 在 Android 批构建前都会调用这里的统一入口。
    /// </summary>
    static public class AndroidExternalToolsBatchResolver
    {
        static private Type androidExternalToolsSettingsType;
        static private bool androidExternalToolsSettingsResolved;

        /// <summary>
        /// 在 BatchMode Android 构建前统一补齐 JDK / SDK / NDK。
        /// 非 BatchMode 场景会直接跳过，不影响编辑器本地手工配置。
        /// </summary>
        static public void EnsureAndroidExternalToolsForBatchMode()
        {
            EnsureAndroidJdkForBatchMode();
            EnsureAndroidSdkForBatchMode();
            EnsureAndroidNdkForBatchMode();
        }

        /// <summary>
        /// 检测给定目录是否为可用 JDK 根目录。
        /// </summary>
        static internal bool IsValidJdkPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

#if UNITY_EDITOR_WIN
            return File.Exists(Path.Combine(path, "bin", "javac.exe"));
#else
            return File.Exists(Path.Combine(path, "bin", "javac"));
#endif
        }

        /// <summary>
        /// 检测给定目录是否为可用 Android SDK 根目录。
        /// </summary>
        static internal bool IsValidAndroidSdkPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

#if UNITY_EDITOR_WIN
            if (File.Exists(Path.Combine(path, "platform-tools", "adb.exe")))
            {
                return true;
            }
#else
            if (File.Exists(Path.Combine(path, "platform-tools", "adb")))
            {
                return true;
            }
#endif

            return Directory.Exists(Path.Combine(path, "platforms")) ||
                   Directory.Exists(Path.Combine(path, "cmdline-tools"));
        }

        /// <summary>
        /// 检测给定目录是否为可用 Android NDK 根目录。
        /// </summary>
        static internal bool IsValidAndroidNdkPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

#if UNITY_EDITOR_WIN
            if (File.Exists(Path.Combine(path, "ndk-build.cmd")))
            {
                return true;
            }
#else
            if (File.Exists(Path.Combine(path, "ndk-build")))
            {
                return true;
            }
#endif

            return File.Exists(Path.Combine(path, "source.properties")) &&
                   Directory.Exists(Path.Combine(path, "toolchains"));
        }

        /// <summary>
        /// 获取当前机器各 Windows 用户目录下常见的 Android SDK 安装候选路径。
        /// </summary>
        static private IEnumerable<string> GetWindowsUserAndroidSdkCandidates()
        {
#if UNITY_EDITOR_WIN
            var usersRoot = @"C:\Users";
            if (!Directory.Exists(usersRoot))
            {
                yield break;
            }

            foreach (var userDir in Directory.GetDirectories(usersRoot))
            {
                yield return Path.Combine(userDir, "AppData", "Local", "Android", "Sdk");
            }
#endif
            yield break;
        }

        /// <summary>
        /// 枚举 Unity 安装目录下内置 Android Support 的候选路径。
        /// </summary>
        static private IEnumerable<string> GetUnityEmbeddedAndroidToolCandidates(params string[] relativePaths)
        {
            var applicationContentsPath = EditorApplication.applicationContentsPath;
            if (string.IsNullOrWhiteSpace(applicationContentsPath) || !Directory.Exists(applicationContentsPath))
            {
                yield break;
            }

            foreach (var relativePath in relativePaths)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    continue;
                }

                yield return Path.Combine(applicationContentsPath, relativePath);
            }
        }

        /// <summary>
        /// 获取 Unity Android External Tools 设置类型。
        /// 这里使用轻量反射，是为了兼容未安装 Android 模块的 Unity Editor，避免直接强引用该类型导致 CI 编译失败。
        /// </summary>
        static private Type GetAndroidExternalToolsSettingsType()
        {
            if (androidExternalToolsSettingsResolved)
            {
                return androidExternalToolsSettingsType;
            }

            androidExternalToolsSettingsResolved = true;

            // 基础设施层在这里使用受控反射，只负责探测 Unity 提供的 AndroidExternalToolsSettings。
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var candidate = assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                if (candidate != null)
                {
                    androidExternalToolsSettingsType = candidate;
                    break;
                }
            }

            if (androidExternalToolsSettingsType == null)
            {
                Debug.LogWarning("【CI】当前 Unity Editor 未提供 AndroidExternalToolsSettings，跳过 Android External Tools 自动配置。");
            }

            return androidExternalToolsSettingsType;
        }

        /// <summary>
        /// 获取 Android External Tools 上某个路径属性的反射句柄。
        /// </summary>
        static private bool TryGetAndroidExternalToolsPathProperty(string propertyName,
            out PropertyInfo propertyInfo)
        {
            propertyInfo = GetAndroidExternalToolsSettingsType()?.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Static);
            return propertyInfo != null && propertyInfo.PropertyType == typeof(string);
        }

        /// <summary>
        /// 读取 Android External Tools 上当前配置的某个路径值。
        /// </summary>
        static private string GetAndroidExternalToolsPath(string propertyName)
        {
            if (!TryGetAndroidExternalToolsPathProperty(propertyName, out var propertyInfo))
            {
                return string.Empty;
            }

            return propertyInfo.GetValue(null) as string ?? string.Empty;
        }

        /// <summary>
        /// 尝试把探测到的候选路径写回 Unity Android External Tools 设置。
        /// </summary>
        static private bool TrySetAndroidExternalToolsPath(string propertyName, string candidate, string source,
            string toolName)
        {
            if (!TryGetAndroidExternalToolsPathProperty(propertyName, out var propertyInfo))
            {
                return false;
            }

            try
            {
                propertyInfo.SetValue(null, candidate);
                Debug.Log($"【CI】已为 Unity Android External Tools 配置 {toolName}({source}): {GetAndroidExternalToolsPath(propertyName)}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】{toolName} 候选路径被 Unity 拒绝({source}): {candidate}，原因: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试把 JDK 候选路径写回 Unity Android External Tools。
        /// </summary>
        static private bool TryApplyAndroidJdkPath(string candidate, string source)
        {
            if (!IsValidJdkPath(candidate))
            {
                return false;
            }

            return TrySetAndroidExternalToolsPath("jdkRootPath", candidate, source, "JDK");
        }

        /// <summary>
        /// 按“环境变量 -> Unity 内置 Android Support -> 本机常见安装目录”的顺序探测可用 JDK。
        /// </summary>
        static private bool TryConfigureAndroidJdkFromCandidates()
        {
            var envNames = new[]
            {
                "UNITY_JDK_PATH",
                "UNITY_JDK",
                "JDK_HOME",
                "JAVA_HOME",
                "TEAMCITY_JRE",
            };

            foreach (var envName in envNames)
            {
                var candidate = System.Environment.GetEnvironmentVariable(envName);
                if (TryApplyAndroidJdkPath(candidate, $"环境变量 {envName}"))
                {
                    return true;
                }
            }

            foreach (var candidate in GetUnityEmbeddedAndroidToolCandidates(
                         Path.Combine("PlaybackEngines", "AndroidPlayer", "OpenJDK"),
                         Path.Combine("PlaybackEngines", "AndroidPlayer", "Tools", "OpenJDK")))
            {
                if (TryApplyAndroidJdkPath(candidate, "Unity 内置 Android Support"))
                {
                    return true;
                }
            }

#if UNITY_EDITOR_WIN
            var roots = new[]
            {
                @"C:\Program Files\Java",
                @"C:\Program Files\OpenJDK",
                @"C:\Program Files\Zulu",
                @"C:\Program Files\Azul",
                @"C:\Program Files\Microsoft",
                @"C:\Program Files\Android\Android Studio",
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var candidate in Directory.GetDirectories(root))
                {
                    if (TryApplyAndroidJdkPath(candidate, "自动探测"))
                    {
                        return true;
                    }
                }

                if (TryApplyAndroidJdkPath(root, "自动探测"))
                {
                    return true;
                }
            }
#endif

            return false;
        }

        /// <summary>
        /// 尝试把 SDK 候选路径写回 Unity Android External Tools。
        /// </summary>
        static private bool TryApplyAndroidSdkPath(string candidate, string source)
        {
            if (!IsValidAndroidSdkPath(candidate))
            {
                return false;
            }

            return TrySetAndroidExternalToolsPath("sdkRootPath", candidate, source, "SDK");
        }

        /// <summary>
        /// 尝试把 NDK 候选路径写回 Unity Android External Tools。
        /// </summary>
        static private bool TryApplyAndroidNdkPath(string candidate, string source)
        {
            if (!IsValidAndroidNdkPath(candidate))
            {
                return false;
            }

            return TrySetAndroidExternalToolsPath("ndkRootPath", candidate, source, "NDK");
        }

        /// <summary>
        /// 按“环境变量 -> Unity 内置 Android Support -> 本机常见安装目录”的顺序探测可用 Android SDK。
        /// </summary>
        static private bool TryConfigureAndroidSdkFromCandidates()
        {
            var envNames = new[]
            {
                "UNITY_ANDROID_SDK",
                "ANDROID_SDK_ROOT",
                "ANDROID_HOME",
            };

            foreach (var envName in envNames)
            {
                var candidate = System.Environment.GetEnvironmentVariable(envName);
                if (TryApplyAndroidSdkPath(candidate, $"环境变量 {envName}"))
                {
                    return true;
                }
            }

            foreach (var candidate in GetUnityEmbeddedAndroidToolCandidates(
                         Path.Combine("PlaybackEngines", "AndroidPlayer", "SDK")))
            {
                if (TryApplyAndroidSdkPath(candidate, "Unity 内置 Android Support"))
                {
                    return true;
                }
            }

#if UNITY_EDITOR_WIN
            var roots = new List<string>
            {
                @"C:\Android\Sdk",
                @"D:\Android\Sdk",
                @"C:\Program Files\Android\Android Studio\sdk",
                @"C:\Program Files\Android\Android Studio\Sdk",
            };
            roots.AddRange(GetWindowsUserAndroidSdkCandidates());

            foreach (var root in roots)
            {
                if (TryApplyAndroidSdkPath(root, "自动探测"))
                {
                    return true;
                }
            }
#endif

            return false;
        }

        /// <summary>
        /// 按“环境变量 -> Unity 内置 Android Support -> SDK 派生路径 -> 本机常见安装目录”的顺序探测可用 Android NDK。
        /// </summary>
        static private bool TryConfigureAndroidNdkFromCandidates()
        {
            var envNames = new[]
            {
                "UNITY_ANDROID_NDK",
                "ANDROID_NDK_ROOT",
                "ANDROID_NDK_HOME",
                "NDK_ROOT",
                "NDK_HOME",
            };

            foreach (var envName in envNames)
            {
                var candidate = System.Environment.GetEnvironmentVariable(envName);
                if (TryApplyAndroidNdkPath(candidate, $"环境变量 {envName}"))
                {
                    return true;
                }
            }

            foreach (var candidate in GetUnityEmbeddedAndroidToolCandidates(
                         Path.Combine("PlaybackEngines", "AndroidPlayer", "NDK")))
            {
                if (TryApplyAndroidNdkPath(candidate, "Unity 内置 Android Support"))
                {
                    return true;
                }
            }

            var sdkRootPath = GetAndroidExternalToolsPath("sdkRootPath");
            if (IsValidAndroidSdkPath(sdkRootPath))
            {
                var sdkNdkBundle = Path.Combine(sdkRootPath, "ndk-bundle");
                if (TryApplyAndroidNdkPath(sdkNdkBundle, "SDK 派生路径"))
                {
                    return true;
                }

                var sdkNdkRoot = Path.Combine(sdkRootPath, "ndk");
                if (Directory.Exists(sdkNdkRoot))
                {
                    foreach (var candidate in Directory.GetDirectories(sdkNdkRoot))
                    {
                        if (TryApplyAndroidNdkPath(candidate, "SDK 派生路径"))
                        {
                            return true;
                        }
                    }
                }
            }

#if UNITY_EDITOR_WIN
            var roots = new[]
            {
                @"C:\Android\Sdk\ndk-bundle",
                @"D:\Android\Sdk\ndk-bundle",
                @"C:\Android\Ndk",
                @"D:\Android\Ndk",
            };

            foreach (var root in roots)
            {
                if (TryApplyAndroidNdkPath(root, "自动探测"))
                {
                    return true;
                }

                if (Directory.Exists(root))
                {
                    foreach (var candidate in Directory.GetDirectories(root))
                    {
                        if (TryApplyAndroidNdkPath(candidate, "自动探测"))
                        {
                            return true;
                        }
                    }
                }
            }
#endif

            return false;
        }

        /// <summary>
        /// 在 BatchMode Android 构建前确保 Unity 已配置可用 JDK。
        /// </summary>
        static private void EnsureAndroidJdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            var jdkRootPath = GetAndroidExternalToolsPath("jdkRootPath");
            if (IsValidJdkPath(jdkRootPath))
            {
                Debug.Log($"【CI】Unity Android JDK 已配置: {jdkRootPath}");
                return;
            }

            if (TryConfigureAndroidJdkFromCandidates())
            {
                return;
            }

            Debug.LogWarning("【CI】未找到可用 JDK；如果 TeamCity Agent 已安装 JDK，请设置 JAVA_HOME/JDK_HOME/UNITY_JDK_PATH。");
        }

        /// <summary>
        /// 在 BatchMode Android 构建前确保 Unity 已配置可用 Android SDK。
        /// </summary>
        static private void EnsureAndroidSdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            var sdkRootPath = GetAndroidExternalToolsPath("sdkRootPath");
            if (IsValidAndroidSdkPath(sdkRootPath))
            {
                Debug.Log($"【CI】Unity Android SDK 已配置: {sdkRootPath}");
                return;
            }

            if (TryConfigureAndroidSdkFromCandidates())
            {
                return;
            }

            Debug.LogWarning("【CI】未找到可用 Android SDK；如果 TeamCity Agent 已安装 SDK，请设置 ANDROID_SDK_ROOT 或 ANDROID_HOME。");
        }

        /// <summary>
        /// 在 BatchMode Android 构建前确保 Unity 已配置可用 Android NDK。
        /// </summary>
        static private void EnsureAndroidNdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            var ndkRootPath = GetAndroidExternalToolsPath("ndkRootPath");
            if (IsValidAndroidNdkPath(ndkRootPath))
            {
                Debug.Log($"【CI】Unity Android NDK 已配置: {ndkRootPath}");
                return;
            }

            if (TryConfigureAndroidNdkFromCandidates())
            {
                return;
            }

            Debug.LogWarning("【CI】未找到可用 Android NDK；如果 TeamCity Agent 已安装 NDK，请设置 ANDROID_NDK_ROOT/ANDROID_NDK_HOME/NDK_ROOT。");
        }
    }
}
