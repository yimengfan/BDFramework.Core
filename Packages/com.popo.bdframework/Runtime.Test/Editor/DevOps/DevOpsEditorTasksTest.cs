using System;
using System.IO;
using BDFramework.Editor.DevOps;
using NUnit.Framework;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// 验证 DevOpsEditorTasks 在普通仓库和 linked worktree 下都能定位到正确的 hooks 目录。
    /// 这些断言只覆盖纯文件系统解析逻辑，不依赖真实 Git 命令。
    /// </summary>
    public class DevOpsEditorTasksTest
    {
        /// <summary>
        /// 验证普通仓库会继续把 hooks 写到工程根目录下的 .git/hooks。
        /// </summary>
        [Test]
        public void ResolveGitHooksDirectory_UsesProjectGitHooksForRegularRepository()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                Directory.CreateDirectory(Path.Combine(tempRoot, ".git"));

                var resolvedDirectory = DevOpsEditorTasks.ResolveGitHooksDirectory(tempRoot);
                var expectedDirectory = Path.Combine(tempRoot, ".git", "hooks");

                Assert.That(NormalizePath(resolvedDirectory), Is.EqualTo(NormalizePath(expectedDirectory)));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证 linked worktree 的 .git 指针文件会进一步解析 commondir，并最终写入共享 hooks 目录。
        /// </summary>
        [Test]
        public void ResolveGitHooksDirectory_UsesCommonGitHooksForWorktreePointer()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var projectRoot = Path.Combine(tempRoot, "android-worktree");
                var commonGitDirectory = Path.Combine(tempRoot, "main-repo", ".git");
                var worktreeGitDirectory = Path.Combine(commonGitDirectory, "worktrees", "android");

                Directory.CreateDirectory(projectRoot);
                Directory.CreateDirectory(worktreeGitDirectory);
                Directory.CreateDirectory(commonGitDirectory);

                File.WriteAllText(Path.Combine(projectRoot, ".git"), "gitdir: ../main-repo/.git/worktrees/android\n");
                File.WriteAllText(Path.Combine(worktreeGitDirectory, "commondir"), "../..\n");

                var resolvedDirectory = DevOpsEditorTasks.ResolveGitHooksDirectory(projectRoot);
                var expectedDirectory = Path.Combine(commonGitDirectory, "hooks");

                Assert.That(NormalizePath(resolvedDirectory), Is.EqualTo(NormalizePath(expectedDirectory)));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 验证当 .git 指针文件没有 commondir 时，会回退到 gitdir/hooks，兼容非 worktree 的自定义 gitdir 布局。
        /// </summary>
        [Test]
        public void ResolveGitHooksDirectory_FallsBackToGitdirHooksWhenCommondirIsMissing()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var projectRoot = Path.Combine(tempRoot, "custom-gitdir-project");
                var redirectedGitDirectory = Path.Combine(tempRoot, "git-store", "project-admin");

                Directory.CreateDirectory(projectRoot);
                Directory.CreateDirectory(redirectedGitDirectory);
                File.WriteAllText(Path.Combine(projectRoot, ".git"), $"gitdir: {redirectedGitDirectory}{Environment.NewLine}");

                var resolvedDirectory = DevOpsEditorTasks.ResolveGitHooksDirectory(projectRoot);
                var expectedDirectory = Path.Combine(redirectedGitDirectory, "hooks");

                Assert.That(NormalizePath(resolvedDirectory), Is.EqualTo(NormalizePath(expectedDirectory)));
            }
            finally
            {
                DeleteDirectoryIfExists(tempRoot);
            }
        }

        /// <summary>
        /// 为每个测试创建独立的临时目录，避免不同仓库布局互相污染。
        /// </summary>
        /// <returns>当前测试可独占使用的临时目录。</returns>
        private static string CreateTempDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "BDFramework-DevOpsEditorTasksTest",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        /// <summary>
        /// 清理测试目录，避免临时仓库布局残留到下一次执行。
        /// </summary>
        /// <param name="directoryPath">待删除的临时目录。</param>
        private static void DeleteDirectoryIfExists(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        /// <summary>
        /// 统一规范路径字符串，避免不同平台的分隔符和尾部斜杠影响断言。
        /// </summary>
        /// <param name="path">待比较的原始路径。</param>
        /// <returns>适合断言比较的规范化绝对路径。</returns>
        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}