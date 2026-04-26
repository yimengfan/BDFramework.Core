using System;
using System.Collections.Generic;
using System.Linq;
using Talos.E2E.Transport;
using UnityEngine;
using UnityEngine.Scripting;

namespace Talos.E2E
{
    /// <summary>
    /// Talos 运行时启动参数辅助器。
    /// Talos runtime launch-argument helper.
    /// 该辅助器只保留 Android `unity` extra 的通用切分与快照能力，
    /// 不再承载 app 启动阶段的外部参数读取策略。
    /// This helper keeps only the generic snapshot/tokenization behavior around the Android `unity` extra
    /// and no longer owns any app-startup policy that reads external launch arguments.
    /// </summary>
    [Preserve]
    public static class RuntimeLaunchArguments
    {
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
        /// 当前只需要保留简单的空白分隔切分能力，供 Android `unity` extra 这类原始字符串进入统一快照。
        /// The current requirement is only whitespace-based tokenization so raw Android `unity` extra strings can enter a unified snapshot.
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
