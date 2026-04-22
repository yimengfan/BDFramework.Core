using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// Talos 运行时启动参数辅助器。
    /// Talos runtime launch-argument helper.
    /// 该辅助器统一整理当前进程可见的 Talos 启动参数来源：
    /// 常规命令行参数，以及 Android Player 通过 Intent `unity` extra 传入的补充参数。
    /// This helper unifies the Talos launch-argument sources visible to the current process:
    /// the normal command-line arguments and the extra arguments passed through the Android Player Intent `unity` extra.
    /// </summary>
    [Preserve]
    public static class RuntimeLaunchArguments
    {
        /// <summary>
        /// 为当前进程构建 Talos 启动参数快照。
        /// Build a Talos launch-argument snapshot for the current process.
        /// 先保留 `Environment.GetCommandLineArgs()` 的原始结果，再把 Android Intent `unity` extra 解析后的令牌追加到末尾，
        /// 这样常规平台维持现状，而 Android 可以补上 Unity 不一定放进命令行数组的额外参数。
        /// It preserves the raw `Environment.GetCommandLineArgs()` result first and then appends the tokens parsed from the Android Intent `unity` extra,
        /// so non-Android platforms keep their current behavior while Android can recover extra arguments that Unity does not always surface in the command-line array.
        /// </summary>
        /// <returns>合并后的参数快照。The merged argument snapshot.</returns>
        public static string[] ResolveCurrentProcessArguments()
        {
            var androidUnityExtra = ReadAndroidUnityExtra();
            if (!string.IsNullOrWhiteSpace(androidUnityExtra))
            {
                Debug.Log($"[TalosE2E] 已解析 Android Intent unity 参数: {androidUnityExtra}");
            }

            return BuildArgumentSnapshot(Environment.GetCommandLineArgs(), androidUnityExtra);
        }

        /// <summary>
        /// 合并环境命令行参数与 Android `unity` extra 参数。
        /// Merge the environment command-line arguments with Android `unity` extra arguments.
        /// </summary>
        /// <param name="environmentArgs">环境命令行参数。Environment command-line arguments.</param>
        /// <param name="androidUnityExtra">Android Intent `unity` extra 的原始字符串。Raw string from the Android Intent `unity` extra.</param>
        /// <returns>合并后的参数快照。The merged argument snapshot.</returns>
        public static string[] BuildArgumentSnapshot(IEnumerable<string> environmentArgs, string androidUnityExtra = null)
        {
            var snapshot = new List<string>();
            if (environmentArgs != null)
            {
                snapshot.AddRange(environmentArgs.Where(arg => !string.IsNullOrWhiteSpace(arg)));
            }

            snapshot.AddRange(TokenizeRawArguments(androidUnityExtra));
            return snapshot.ToArray();
        }

        /// <summary>
        /// 检查参数序列里是否存在指定参数名。
        /// Check whether the argument sequence contains the specified argument name.
        /// </summary>
        /// <param name="args">参数序列。Argument sequence.</param>
        /// <param name="argumentName">参数名。Argument name.</param>
        /// <returns>命中时返回 true，否则返回 false。Returns true when the argument is present; otherwise false.</returns>
        public static bool ContainsArgument(IEnumerable<string> args, string argumentName)
        {
            if (args == null || string.IsNullOrWhiteSpace(argumentName))
            {
                return false;
            }

            foreach (var arg in args)
            {
                if (string.Equals(arg, argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 读取指定参数名后面的参数值。
        /// Read the argument value that follows the specified argument name.
        /// </summary>
        /// <param name="args">参数序列。Argument sequence.</param>
        /// <param name="argumentName">参数名。Argument name.</param>
        /// <param name="value">命中时返回参数值。Returns the argument value when found.</param>
        /// <returns>成功命中参数和值时返回 true，否则返回 false。Returns true when both the argument and its value are found; otherwise false.</returns>
        public static bool TryGetArgumentValue(IEnumerable<string> args, string argumentName, out string value)
        {
            value = string.Empty;
            if (args == null || string.IsNullOrWhiteSpace(argumentName))
            {
                return false;
            }

            var snapshot = args as string[] ?? args.ToArray();
            for (var index = 0; index < snapshot.Length - 1; index++)
            {
                if (!string.Equals(snapshot[index], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = snapshot[index + 1];
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        /// <summary>
        /// 读取 Android Player Intent 里的 `unity` extra。
        /// Read the Android Player `unity` extra from the current Intent.
        /// </summary>
        /// <returns>命中时返回原始参数串，否则返回空字符串。Returns the raw argument string when present; otherwise an empty string.</returns>
        public static string ReadAndroidUnityExtra()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = currentActivity?.Call<AndroidJavaObject>("getIntent"))
                {
                    if (intent == null)
                    {
                        return string.Empty;
                    }

                    return intent.Call<string>("getStringExtra", "unity") ?? string.Empty;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TalosE2E] 读取 Android Intent unity 参数失败: {exception.GetType().FullName}: {exception.Message}");
            }
#endif

            return string.Empty;
        }

        /// <summary>
        /// 将原始参数字符串切分为参数令牌。
        /// Tokenize a raw argument string into individual argument tokens.
        /// 当前 Talos Android 启动链只传递简单的空白分隔参数，
        /// 因而这里使用空白切分就足以覆盖 `-talosPort 10002 -talosForceE2E` 这类输入。
        /// The current Talos Android launch path passes only simple whitespace-separated arguments,
        /// so whitespace tokenization is sufficient for inputs such as `-talosPort 10002 -talosForceE2E`.
        /// </summary>
        /// <param name="rawArguments">原始参数字符串。Raw argument string.</param>
        /// <returns>切分后的参数令牌。Tokenized argument array.</returns>
        public static string[] TokenizeRawArguments(string rawArguments)
        {
            if (string.IsNullOrWhiteSpace(rawArguments))
            {
                return Array.Empty<string>();
            }

            return rawArguments.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}