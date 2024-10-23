using System.Diagnostics;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.Task;
using UnityEditor;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// devops的editor任务
    /// </summary>
    public class DevOpsEditorTasks
    {
        /// <summary>
        /// 更新GitHook到本地仓库
        /// </summary>
        [EditorTask.EditorTaskOnUnityLoadOrCodeRecompiled("同步githook文件到本地仓库")]
        static public void UpdateGitHookToLocalStore()
        {
            var projectGithookDir = Path.Combine(BApplication.DevOpsCIPath, "githook");
            var localStoreGitdir = Path.Combine(BApplication.ProjectRoot, ".git/hooks");
            //清空本地
            if (Directory.Exists(localStoreGitdir))
            {
                Directory.Delete(localStoreGitdir,true);
            }
            Directory.CreateDirectory(localStoreGitdir);
            //复制
            FileHelper.CopyFolderTo(projectGithookDir,localStoreGitdir);
        }

    }
}


