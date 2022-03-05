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
            var githookDir = Path.Combine(BDApplication.DevOpsCIPath, "githook");
            var gitdir = Path.Combine(BDApplication.ProjectRoot, ".git/hooks");
            if (Directory.Exists(gitdir) && Directory.Exists(githookDir))
            {
                var hookfiles = Directory.GetFiles(githookDir, "*", SearchOption.AllDirectories);

                foreach (var hookfile in hookfiles)
                {
                    var filename = Path.GetFileName(hookfile);
                    var copytodir = Path.Combine(gitdir, filename);
                    //覆盖拷贝
                    File.Copy(hookfile, copytodir, true);
                }
            }
        }
        
    }
}


