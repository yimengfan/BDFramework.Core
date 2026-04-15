using BDFramework.Editor.Tools;
using System;
using Debug = UnityEngine.Debug;
namespace Game.Editor.PublishPipeline
{

    /// <summary>
    /// git处理器
    /// </summary>
    public class GitProcessor
    {

        /// <summary>
        /// 获取当前git分支名
        /// </summary>
        /// <returns></returns>
        static public string GetBranchName()
        {
            var cmd = $"git branch --show-current";
            var cmdret = CMDTools.RunCmd(cmd);


#if UNITY_EDITOR_OSX
            return cmdret;
#elif UNITY_EDITOR_WIN
            string[] getstr = cmdret.Split(new string[] { "\n" }, StringSplitOptions.None);

            foreach (var item in getstr)
            {
                if (item.StartsWith("dev_"))
                {
                    return item;
                }
            }
            return null;
#endif

        }

        /// <summary>
        /// 获取hash长度
        /// </summary>
        /// <param name="lenth"></param>
        /// <returns></returns>
        static public string GetVersion(int lenth)
        {
            var cmdret = CMDTools.RunCmd("git log -1 --pretty=format:%H");
          
            if (lenth <= 5)
            {
                lenth = 5;
            }

#if UNITY_EDITOR_OSX
            return cmdret.Substring(0, lenth);
#elif UNITY_EDITOR_WIN
            var strarry = cmdret.Split(new string[] { "\n" }, StringSplitOptions.None);
            return strarry[4].Substring(0, lenth);
#endif
            
            
            Debug.Log(cmdret);
          
        }

       // [MenuItem("Test/TestGitCmd")]
        static public void GetVersion()
        {
            Debug.Log(GetBranchName());
            var hash = GetVersion(5);
            Debug.Log(hash);
        }


    }
}
