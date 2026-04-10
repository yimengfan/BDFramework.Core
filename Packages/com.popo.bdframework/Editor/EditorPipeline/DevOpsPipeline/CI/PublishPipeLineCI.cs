using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Environment;
using BDFramework.Editor.Table;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// BDFramework Editor 侧的 CI 发布入口集合。
    /// 该类把母包、热更代码、AssetBundle 和表格的 BatchMode 入口集中到同一个协调器里，供 TeamCity 或命令行通过 <c>-executeMethod</c> 调用。
    /// </summary>
    /// <example>
    /// Unity -batchmode -projectPath &lt;project&gt; -executeMethod BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid
    /// </example>
    static public class PublishPipeLineCI
    {
        /// <summary>
        /// BatchMode 下指定 CI 构建输出根目录的命令行参数。
        /// 该目录由 Python / TeamCity 侧构建脚本传入，Unity 构建产物会统一落到这里，供后续上传流程消费。
        /// </summary>
        const string CI_OUTPUT_ROOT_ARG = "-ciOutputRoot";


        static PublishPipeLineCI()
        {
            //TODO : 初始化编辑器,必须
            if (Application.isBatchMode)
            {
                BDFrameworkEditorEnvironment.InitEditorEnvironment();
            }

        }




        #region 代码打包检查

        [CI(Des = "代码检查")]
        public static void CheckEditorCode()
        {
            CheckCode();
        }

        /// <summary>
        /// 检测代码
        /// </summary>
        /// <returns></returns>
        public static bool CheckCode()
        {
            //检查下打包前的代码错
            var setting = new ScriptCompilationSettings();
            setting.options = ScriptCompilationOptions.Assertions;
            setting.target = BuildTarget.Android;
            var ret = PlayerBuildInterface.CompilePlayerScripts(setting, BApplication.Library + "/BuildTest");
            if (ret.assemblies.Contains("Assembly-CSharp.dll"))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// 构建dll
        /// </summary>
        public static void BuildDLL()
        {
          }

        #endregion

        #region 发布母包

        const string CLIENT_VERSION_ARG = "-clientVersion";

        /// <summary>
        /// 从当前 Unity 进程命令行中读取指定参数值。
        /// </summary>
        static private string GetCommandLineArg(string argName)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        /// <summary>
        /// 解析本次 CI 构建应使用的 clientVersion。
        /// 如果命令行没有显式传入，就回退到 BuildTools_ClientPackage 的默认版本号规则。
        /// </summary>
        static private string GetClientVersion()
        {
            var clientVersion = GetCommandLineArg(CLIENT_VERSION_ARG);
            if (!string.IsNullOrWhiteSpace(clientVersion))
            {
                return clientVersion.Trim();
            }

            return BuildTools_ClientPackage.GetDefaultClientVersion();
        }

        static private bool IsValidJdkPath(string path)
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

        static private bool IsValidAndroidSdkPath(string path)
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

            return Directory.Exists(Path.Combine(path, "platforms")) || Directory.Exists(Path.Combine(path, "cmdline-tools"));
        }

        static private bool IsValidAndroidNdkPath(string path)
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

            return File.Exists(Path.Combine(path, "source.properties")) && Directory.Exists(Path.Combine(path, "toolchains"));
        }

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

        static private Type androidExternalToolsSettingsType;
        static private bool androidExternalToolsSettingsResolved;

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

            // 框架基础设施层在这里使用受控反射，只负责探测 Unity 提供的 AndroidExternalToolsSettings。
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
            out System.Reflection.PropertyInfo propertyInfo)
        {
            propertyInfo = GetAndroidExternalToolsSettingsType()?.GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
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

        static private bool TryApplyAndroidJdkPath(string candidate, string source)
        {
            if (!IsValidJdkPath(candidate))
            {
                return false;
            }

            try
            {
                return TrySetAndroidExternalToolsPath("jdkRootPath", candidate, source, "JDK");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】JDK 候选路径被 Unity 拒绝({source}): {candidate}，原因: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 按“环境变量 -&gt; Unity 内置 Android Support -&gt; 本机常见安装目录”的顺序探测可用 JDK。
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

        static private bool TryApplyAndroidSdkPath(string candidate, string source)
        {
            if (!IsValidAndroidSdkPath(candidate))
            {
                return false;
            }

            try
            {
                return TrySetAndroidExternalToolsPath("sdkRootPath", candidate, source, "SDK");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】SDK 候选路径被 Unity 拒绝({source}): {candidate}，原因: {exception.Message}");
                return false;
            }
        }

        static private bool TryApplyAndroidNdkPath(string candidate, string source)
        {
            if (!IsValidAndroidNdkPath(candidate))
            {
                return false;
            }

            try
            {
                return TrySetAndroidExternalToolsPath("ndkRootPath", candidate, source, "NDK");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】NDK 候选路径被 Unity 拒绝({source}): {candidate}，原因: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 按“环境变量 -&gt; Unity 内置 Android Support -&gt; 本机常见安装目录”的顺序探测可用 Android SDK。
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
        /// 按“环境变量 -&gt; Unity 内置 Android Support -&gt; SDK 派生路径 -&gt; 本机常见安装目录”的顺序探测可用 Android NDK。
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

        /// <summary>
        /// 发布包体 AndroidDebug
        /// </summary>
        [CI(Des = "发布母包Android-Debug")]
        static public void PublishPackage_AndroidDebug()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 AndroidRelease
        /// </summary>
        [CI(Des = "发布母包Android-Release")]
        static public void PublishPackage_AndroidRelease()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 iOSDebug
        /// </summary>
        [CI(Des = "发布母包iOS-Debug")]
        static public void PublishPackage_iOSDebug()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 iOSRelease
        /// </summary>
        [CI(Des = "发布母包iOS-Release")]
        static public void PublishPackage_iOSRelease()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// 发布包体 WindowsDebug
        /// </summary>
        [CI(Des = "发布母包Windows-Debug")]
        static public void PublishPackage_WindowsDebug()
        {
            BuildPackage(BuildTarget.StandaloneWindows64, BuildTools_ClientPackage.BuildMode.Debug);
        }

        /// <summary>
        /// 发布包体 WindowsRelease
        /// </summary>
        [CI(Des = "发布母包Windows-Release")]
        static public void PublishPackage_WindowsRelease()
        {
            BuildPackage(BuildTarget.StandaloneWindows64, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 Android Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Android-Release")]
        static public void BuildClientPackageAndroid()
        {
            BuildPackage(BuildTarget.Android, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 iOS Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包iOS-Release")]
        static public void BuildClientPackageIOS()
        {
            BuildPackage(BuildTarget.iOS, BuildTools_ClientPackage.BuildMode.Release);
        }

        /// <summary>
        /// BatchMode: 构建 Windows Release 母包
        /// </summary>
        [CI(Des = "BatchMode构建母包Windows-Release")]
        static public void BuildClientPackageWindows()
        {
            BuildPackage(BuildTarget.StandaloneWindows64, BuildTools_ClientPackage.BuildMode.Release);
        }


        /// <summary>
        /// 解析本次 CI 构建的产物根目录。
        /// 优先读取命令行里的 <c>-ciOutputRoot</c>，否则回退到框架默认的发布目录。
        /// </summary>
        static private string GetCIOutputRoot(string defaultOutputRoot)
        {
            var outputRoot = GetCommandLineArg(CI_OUTPUT_ROOT_ARG);
            if (!string.IsNullOrWhiteSpace(outputRoot))
            {
                var resolvedOutputRoot = Path.GetFullPath(outputRoot.Trim());
                Directory.CreateDirectory(resolvedOutputRoot);
                return resolvedOutputRoot;
            }

            Directory.CreateDirectory(defaultOutputRoot);
            return defaultOutputRoot;
        }


        /// <summary>
        /// 清理一个可选存在的目录，并输出显式日志说明清理来源。
        /// </summary>
        static private void DeleteDirectoryIfExists(string path, string description)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return;
            }

            Debug.Log($"【CI】清理{description}:{path}");
            Directory.Delete(path, true);
        }


        /// <summary>
        /// 在 BatchMode AssetBundle 构建前清理 Unity 与 AssetGraph 的缓存目录。
        /// 这样可以避免 CI 连续构建时复用到陈旧缓存，导致热更资源结果不一致。
        /// </summary>
        static private void PrepareBatchModeAssetbundleCaches()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            Debug.Log("【CI】准备清理Assetbundle构建缓存");
            BuildCache.PurgeCache(false);
            DeleteDirectoryIfExists(UnityEngine.AssetGraph.DataModel.Version2.Settings.Path.CachePath, "AssetGraph缓存");
            DeleteDirectoryIfExists(UnityEngine.AssetGraph.DataModel.Version2.Settings.Path.BundleBuilderCachePath, "AssetGraph BundleBuilder缓存");
            AssetDatabase.Refresh();
        }


        /// <summary>
        /// 构建指定平台的母包。
        /// </summary>
        static private void BuildPackage(BuildTarget buildTarget, BuildTools_ClientPackage.BuildMode buildMode)
        {
            // Phase 1: 解析本次构建使用的版本号，并在 Android 平台提前补齐 Unity External Tools。
            var clientVersion = GetClientVersion();
            Debug.Log($"【CI】BuildTarget:{buildTarget} BuildMode:{buildMode} ClientVersion:{clientVersion}");

            if (buildTarget == BuildTarget.Android)
            {
                EnsureAndroidJdkForBatchMode();
                EnsureAndroidSdkForBatchMode();
                EnsureAndroidNdkForBatchMode();
            }

            // Phase 2: 统一委托现有母包构建管线执行真正的打包逻辑。
            var ret = BuildTools_ClientPackage.Build(
                buildMode,
                true,
                BApplication.DevOpsPublishClientPackagePath,
                buildTarget,
                BuildTools_Assets.BuildPackageOption.BuildAll,
                clientVersion);

            if (!ret)
            {
                throw new Exception($"【CI】构建母包失败! Target:{buildTarget} Mode:{buildMode} ClientVersion:{clientVersion}");
            }
        }


        /// <summary>
        /// 构建指定平台的热更资源产物。
        /// 根据传入的构建选项，这里会协调热更代码、AssetBundle 或两者组合的 CI 输出路径和前置清理步骤。
        /// </summary>
        static private void BuildClientRes(BuildTarget buildTarget, BuildTools_Assets.BuildPackageOption buildOption)
        {
            // Phase 1: 解析版本号、输出目录和运行平台，并为 Android 补齐 External Tools。
            var clientVersion = GetClientVersion();
            var outputRoot = GetCIOutputRoot(BApplication.DevOpsPublishAssetsPath);
            var platform = BApplication.GetRuntimePlatform(buildTarget);
            Debug.Log($"【CI】BuildClientRes Target:{buildTarget} Platform:{platform} Option:{buildOption} ClientVersion:{clientVersion} OutputRoot:{outputRoot}");

            if (buildTarget == BuildTarget.Android)
            {
                EnsureAndroidJdkForBatchMode();
                EnsureAndroidSdkForBatchMode();
                EnsureAndroidNdkForBatchMode();
            }

            if (buildOption.HasFlag(BuildTools_Assets.BuildPackageOption.BuildArtAssets))
            {
                PrepareBatchModeAssetbundleCaches();
            }

            // Phase 2: 统一委托 BuildTools_Assets 输出 CI 需要消费的热更制品目录。
            BuildTools_Assets.BuildAll(
                platform,
                outputRoot,
                setNewVersionNum: clientVersion,
                opa: buildOption);
        }

        /// <summary>
        /// BatchMode: 构建 Android 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码Android")]
        static public void BuildCodeAndroid()
        {
            BuildClientRes(BuildTarget.Android, BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
        }

        /// <summary>
        /// BatchMode: 构建 iOS 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码iOS")]
        static public void BuildCodeIOS()
        {
            BuildClientRes(BuildTarget.iOS, BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
        }

        /// <summary>
        /// BatchMode: 构建 Windows 热更代码。
        /// </summary>
        [CI(Des = "BatchMode构建热更代码Windows")]
        static public void BuildCodeWindows()
        {
            BuildClientRes(BuildTarget.StandaloneWindows64, BuildTools_Assets.BuildPackageOption.BuildHotfixCode);
        }

        /// <summary>
        /// BatchMode: 构建 Android 热更 AssetBundle。
        /// </summary>
        [CI(Des = "BatchMode构建热更Assetbundle Android")]
        static public void BuildAssetbundleAndroid()
        {
            BuildClientRes(BuildTarget.Android, BuildTools_Assets.BuildPackageOption.BuildArtAssets);
        }

        /// <summary>
        /// BatchMode: 构建 iOS 热更 AssetBundle。
        /// </summary>
        [CI(Des = "BatchMode构建热更Assetbundle iOS")]
        static public void BuildAssetbundleIOS()
        {
            BuildClientRes(BuildTarget.iOS, BuildTools_Assets.BuildPackageOption.BuildArtAssets);
        }

        /// <summary>
        /// BatchMode: 构建 Windows 热更 AssetBundle。
        /// </summary>
        [CI(Des = "BatchMode构建热更Assetbundle Windows")]
        static public void BuildAssetbundleWindows()
        {
            BuildClientRes(BuildTarget.StandaloneWindows64, BuildTools_Assets.BuildPackageOption.BuildArtAssets);
        }

        /// <summary>
        /// BatchMode: 构建统一表格数据库。
        /// 产物会写到 CI 输出根目录，供后续上传脚本发布为共享 Table 制品。
        /// </summary>
        [CI(Des = "BatchMode构建统一表格")]
        static public void BuildTable()
        {
            // Phase 1: 解析输出目录并记录运行平台，确保 client.db / server.db 会落到 CI 约定目录。
            var outputRoot = GetCIOutputRoot(BApplication.DevOpsPublishAssetsPath);
            var platform = BApplication.RuntimePlatform;
            Debug.Log($"【CI】BuildTable Platform:{platform} OutputRoot:{outputRoot}");

            // Phase 2: 依次构建 client.db 与 server.db，任何一步失败都直接中止当前 BatchMode 任务。
            var buildClientDb = BuildTools_Excel2SQLite.BuildSqlite(outputRoot, platform, DBType.Local);
            if (!buildClientDb)
            {
                throw new Exception($"【CI】构建 client.db 失败! Platform:{platform} OutputRoot:{outputRoot}");
            }

            var buildServerDb = BuildTools_Excel2SQLite.BuildSqlite(outputRoot, platform, DBType.Server);
            if (!buildServerDb)
            {
                throw new Exception($"【CI】构建 server.db 失败! Platform:{platform} OutputRoot:{outputRoot}");
            }
        }

        #endregion


        
    }
}
