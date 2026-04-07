using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.EditorPipeline.DevOps;
using BDFramework.Editor.Environment;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Player;
using UnityEngine;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 构建相关的CI接口
    /// </summary>
    static public class PublishPipeLineCI
    {


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

        static private bool TryApplyAndroidJdkPath(string candidate, string source)
        {
            if (!IsValidJdkPath(candidate))
            {
                return false;
            }

            try
            {
                AndroidExternalToolsSettings.jdkRootPath = candidate;
                Debug.Log($"【CI】已为 Unity Android External Tools 配置 JDK({source}): {AndroidExternalToolsSettings.jdkRootPath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】JDK 候选路径被 Unity 拒绝({source}): {candidate}，原因: {exception.Message}");
                return false;
            }
        }

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
                AndroidExternalToolsSettings.sdkRootPath = candidate;
                Debug.Log($"【CI】已为 Unity Android External Tools 配置 SDK({source}): {AndroidExternalToolsSettings.sdkRootPath}");
                return true;
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
                AndroidExternalToolsSettings.ndkRootPath = candidate;
                Debug.Log($"【CI】已为 Unity Android External Tools 配置 NDK({source}): {AndroidExternalToolsSettings.ndkRootPath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"【CI】NDK 候选路径被 Unity 拒绝({source}): {candidate}，原因: {exception.Message}");
                return false;
            }
        }

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

            if (IsValidAndroidSdkPath(AndroidExternalToolsSettings.sdkRootPath))
            {
                var sdkNdkBundle = Path.Combine(AndroidExternalToolsSettings.sdkRootPath, "ndk-bundle");
                if (TryApplyAndroidNdkPath(sdkNdkBundle, "SDK 派生路径"))
                {
                    return true;
                }

                var sdkNdkRoot = Path.Combine(AndroidExternalToolsSettings.sdkRootPath, "ndk");
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

        static private void EnsureAndroidJdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            if (IsValidJdkPath(AndroidExternalToolsSettings.jdkRootPath))
            {
                Debug.Log($"【CI】Unity Android JDK 已配置: {AndroidExternalToolsSettings.jdkRootPath}");
                return;
            }

            if (TryConfigureAndroidJdkFromCandidates())
            {
                return;
            }

            Debug.LogWarning("【CI】未找到可用 JDK；如果 TeamCity Agent 已安装 JDK，请设置 JAVA_HOME/JDK_HOME/UNITY_JDK_PATH。");
        }

        static private void EnsureAndroidSdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            if (IsValidAndroidSdkPath(AndroidExternalToolsSettings.sdkRootPath))
            {
                Debug.Log($"【CI】Unity Android SDK 已配置: {AndroidExternalToolsSettings.sdkRootPath}");
                return;
            }

            if (TryConfigureAndroidSdkFromCandidates())
            {
                return;
            }

            Debug.LogWarning("【CI】未找到可用 Android SDK；如果 TeamCity Agent 已安装 SDK，请设置 ANDROID_SDK_ROOT 或 ANDROID_HOME。");
        }

        static private void EnsureAndroidNdkForBatchMode()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            if (IsValidAndroidNdkPath(AndroidExternalToolsSettings.ndkRootPath))
            {
                Debug.Log($"【CI】Unity Android NDK 已配置: {AndroidExternalToolsSettings.ndkRootPath}");
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
        /// 构建包体
        /// </summary>
        static private void BuildPackage(BuildTarget buildTarget, BuildTools_ClientPackage.BuildMode buildMode)
        {
            var clientVersion = GetClientVersion();
            Debug.Log($"【CI】BuildTarget:{buildTarget} BuildMode:{buildMode} ClientVersion:{clientVersion}");

            if (buildTarget == BuildTarget.Android)
            {
                EnsureAndroidJdkForBatchMode();
                EnsureAndroidSdkForBatchMode();
                EnsureAndroidNdkForBatchMode();
            }

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

        #endregion


        
    }
}
