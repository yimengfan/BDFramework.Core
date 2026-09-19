# GitHook自动发布

很多时候 我们需要配置本地的githook，但是需要同步到每个本地版本库中，

所以框架集成了一个自动同步githook功能。

在 **"DevOps\CI\githook"** 下所有的所有文件

会被同步到

Assets同级的 **".git/hooks"** 目录下.

这样只需要将hook文件提交到版本库，所有人更新就会同步进去.

```c# 
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
```
