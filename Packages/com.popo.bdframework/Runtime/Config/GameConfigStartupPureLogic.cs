using System;

namespace BDFramework.Configure
{
    /// <summary>
    /// 启动与配置装载的纯逻辑工具集。
    /// Pure logic helpers for startup and configuration loading.
    /// 这个类型承载不会触发 Unity 场景查找、文件读取或管理器启动副作用的判断逻辑，
    /// 让启动链路的关键分支可以在 EditorTest 和 batchmode 中直接复用生产逻辑验证。
    /// This type hosts decision logic that does not trigger Unity scene lookups, file reads, or manager-start side effects,
    /// so key startup branches can be validated in EditorTest and batchmode by directly reusing production logic.
    /// </summary>
    internal static class GameConfigStartupPureLogic
    {
        /// <summary>
        /// 框架配置文本来源类型。
        /// Framework configuration text source kind.
        /// </summary>
        internal enum FrameworkConfigTextSourceKind
        {
            None,
            RuntimeLauncherTextAsset,
            SceneLauncherTextAsset,
            EditorDefaultFile,
        }

        /// <summary>
        /// 框架配置文本来源决策结果。
        /// Framework configuration text source decision result.
        /// </summary>
        internal sealed class FrameworkConfigTextSourcePlan
        {
            /// <summary>
            /// 本次命中的配置来源类型。
            /// The selected configuration source kind for the current resolution.
            /// </summary>
            public FrameworkConfigTextSourceKind SourceKind { get; set; }

            /// <summary>
            /// 当前来源的可读标识，例如 TextAsset 名称或默认配置文件路径。
            /// A readable identifier for the selected source, such as a TextAsset name or the default config file path.
            /// </summary>
            public string SourceIdentifier { get; set; } = string.Empty;

            /// <summary>
            /// 当前来源是否应该输出统一的配置来源日志。
            /// Whether the current source should emit the unified configuration-source log.
            /// </summary>
            public bool ShouldLogSource { get; set; }
        }

        /// <summary>
        /// 判断当前是否应触发配置管理器正式启动。
        /// Decide whether the configuration manager should be started now.
        /// </summary>
        internal static bool ShouldLoadFrameworkConfigManager(bool hasGameConfigManagerInstance)
        {
            return hasGameConfigManagerInstance;
        }

        /// <summary>
        /// 解析本次框架配置文本应来自哪里。
        /// Resolve where the framework configuration text should come from for the current startup path.
        /// 规则固定为：运行时 launcher 文本优先，其次场景中的 launcher 文本，最后才是编辑器默认 bytes 文件。
        /// The rule is fixed as: runtime launcher text first, then launcher text found in the scene, and finally the editor default bytes file.
        /// </summary>
        internal static FrameworkConfigTextSourcePlan ResolveFrameworkConfigTextSource(
            bool isPlaying,
            bool hasRuntimeLauncherConfigText,
            string runtimeLauncherConfigName,
            bool hasSceneLauncherConfigText,
            string sceneLauncherConfigName,
            bool isEditor,
            bool defaultEditorConfigExists,
            string defaultEditorConfigPath)
        {
            if (isPlaying && hasRuntimeLauncherConfigText)
            {
                return new FrameworkConfigTextSourcePlan()
                {
                    SourceKind = FrameworkConfigTextSourceKind.RuntimeLauncherTextAsset,
                    SourceIdentifier = (runtimeLauncherConfigName ?? string.Empty).Trim(),
                    ShouldLogSource = true,
                };
            }

            if (hasSceneLauncherConfigText)
            {
                return new FrameworkConfigTextSourcePlan()
                {
                    SourceKind = FrameworkConfigTextSourceKind.SceneLauncherTextAsset,
                    SourceIdentifier = (sceneLauncherConfigName ?? string.Empty).Trim(),
                    ShouldLogSource = true,
                };
            }

            if (isEditor && defaultEditorConfigExists)
            {
                return new FrameworkConfigTextSourcePlan()
                {
                    SourceKind = FrameworkConfigTextSourceKind.EditorDefaultFile,
                    SourceIdentifier = (defaultEditorConfigPath ?? string.Empty).Trim(),
                    ShouldLogSource = true,
                };
            }

            return new FrameworkConfigTextSourcePlan()
            {
                SourceKind = FrameworkConfigTextSourceKind.None,
                SourceIdentifier = string.Empty,
                ShouldLogSource = false,
            };
        }

        /// <summary>
        /// 生成统一的配置来源日志文本。
        /// Build the unified configuration-source log message.
        /// 当来源标识缺失时，统一回退到 <c>-</c>，避免启动日志里出现空白来源。
        /// When the source identifier is missing, the message falls back to <c>-</c> so startup logs never show a blank source.
        /// </summary>
        internal static string FormatFrameworkConfigSourceLogMessage(string sourceIdentifier)
        {
            var normalizedSourceIdentifier = (sourceIdentifier ?? string.Empty).Trim();
            return "GameConfig加载配置:" + (string.IsNullOrEmpty(normalizedSourceIdentifier) ? "-" : normalizedSourceIdentifier);
        }
    }
}