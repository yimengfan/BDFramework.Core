using System;
using System.Diagnostics;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.Task;
using UnityEditor;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// 提供 DevOps 编辑器启动阶段的本地仓库维护任务。
    /// 例如 CI worktree 工程初始化时，会在这里把仓库 hooks 同步到真实的 Git hooks 目录。
    /// </summary>
    public class DevOpsEditorTasks
    {
        /// <summary>
        /// 把 DevOps 维护的 hook 文件同步到当前工程实际使用的 Git hooks 目录。
        /// 普通仓库直接写入 <c>.git/hooks</c>；linked worktree 会先解析 <c>.git</c> 指针和 <c>commondir</c>，再写入共享 hooks 目录。
        /// </summary>
        [EditorTask.EditorTaskOnUnityLoadOrCodeRecompiled("同步githook文件到本地仓库")]
        static public void UpdateGitHookToLocalStore()
        {
            var projectGithookDir = Path.Combine(BApplication.DevOpsCIPath, "githook");
            // Phase 1: 解析当前工程真实使用的 hooks 目录，兼容普通仓库和 git worktree。
            var localStoreGitdir = ResolveGitHooksDirectory(BApplication.ProjectRoot);

            // Phase 2: 清空旧 hooks，避免主仓库和 CI worktree 复用旧脚本。
            if (Directory.Exists(localStoreGitdir))
            {
                Directory.Delete(localStoreGitdir, true);
            }
            Directory.CreateDirectory(localStoreGitdir);

            // Phase 3: 复制当前 DevOps 维护的 hooks 到解析后的目标目录。
            FileHelper.CopyFolderTo(projectGithookDir, localStoreGitdir);
        }

        /// <summary>
        /// 解析当前工程实际生效的 Git hooks 目录。
        /// 当工程位于 linked worktree 中时，<c>.git</c> 是一个指针文件，真正的 hooks 位于 common git dir 下。
        /// </summary>
        /// <param name="projectRoot">Unity 工程根目录。</param>
        /// <returns>应该写入 hook 文件的绝对目录。</returns>
        internal static string ResolveGitHooksDirectory(string projectRoot)
        {
            var gitMetadataPath = Path.Combine(projectRoot, ".git");
            if (Directory.Exists(gitMetadataPath))
            {
                return Path.Combine(gitMetadataPath, "hooks");
            }

            if (!File.Exists(gitMetadataPath))
            {
                return Path.Combine(gitMetadataPath, "hooks");
            }

            var gitDirectory = ResolveGitDirectory(projectRoot, gitMetadataPath);
            var commonGitDirectory = ResolveCommonGitDirectory(gitDirectory);
            return Path.Combine(commonGitDirectory, "hooks");
        }

        /// <summary>
        /// 从 worktree 的 <c>.git</c> 指针文件里解析真实 git dir。
        /// 无法识别时回退到工程根目录下的 <c>.git</c> 路径，保持普通仓库的旧行为。
        /// </summary>
        /// <param name="projectRoot">Unity 工程根目录。</param>
        /// <param name="gitPointerFilePath">工程根目录下的 <c>.git</c> 指针文件。</param>
        /// <returns>解析后的 git dir 绝对路径。</returns>
        internal static string ResolveGitDirectory(string projectRoot, string gitPointerFilePath)
        {
            var pointerLines = File.ReadAllLines(gitPointerFilePath);
            if (pointerLines.Length == 0)
            {
                return Path.Combine(projectRoot, ".git");
            }

            var pointerLine = pointerLines[0].Trim();
            const string gitDirPrefix = "gitdir:";
            if (!pointerLine.StartsWith(gitDirPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(projectRoot, ".git");
            }

            var rawGitDirectory = pointerLine.Substring(gitDirPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(rawGitDirectory))
            {
                return Path.Combine(projectRoot, ".git");
            }

            if (!Path.IsPathRooted(rawGitDirectory))
            {
                rawGitDirectory = Path.Combine(projectRoot, rawGitDirectory);
            }

            return Path.GetFullPath(rawGitDirectory);
        }

        /// <summary>
        /// 解析 git dir 对应的 common git dir。
        /// linked worktree 会通过 <c>commondir</c> 指回主仓库的共享元数据目录；普通仓库直接返回原始 git dir。
        /// </summary>
        /// <param name="gitDirectory">worktree 或普通仓库解析得到的 git dir。</param>
        /// <returns>真正承载 hooks 的 common git dir 绝对路径。</returns>
        internal static string ResolveCommonGitDirectory(string gitDirectory)
        {
            var commonDirFilePath = Path.Combine(gitDirectory, "commondir");
            if (!File.Exists(commonDirFilePath))
            {
                return gitDirectory;
            }

            var rawCommonDirectory = File.ReadAllText(commonDirFilePath).Trim();
            if (string.IsNullOrWhiteSpace(rawCommonDirectory))
            {
                return gitDirectory;
            }

            if (!Path.IsPathRooted(rawCommonDirectory))
            {
                rawCommonDirectory = Path.Combine(gitDirectory, rawCommonDirectory);
            }

            return Path.GetFullPath(rawCommonDirectory);
        }

    }
}


