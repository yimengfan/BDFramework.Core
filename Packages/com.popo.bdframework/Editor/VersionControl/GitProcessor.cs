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
        /// <returns>hash 前缀；不可得时回退 "0"（与 Get*SVCNum 的默认值语义一致）</returns>
        static public string GetVersion(int lenth)
        {
            if (lenth <= 5)
            {
                lenth = 5;
            }

            var cmdret = CMDTools.RunCmd("git log -1 --pretty=format:%H");
            var hash = FindCommitHash(cmdret, lenth);
            if (hash != null)
            {
                return hash;
            }

            //无 git 仓库 / 无提交 / cmd 输出行序变化时不再越界崩溃，回退 "0" 并告警
            Debug.LogWarning("[GitProcessor] 未取到 git 提交 hash（无仓库/无提交或输出异常），SVC 版本回退为 \"0\"");
            return "0";
        }

        /// <summary>
        /// 在 cmd 输出中查找提交 hash 行并截取前缀：
        /// 优先匹配 40 位十六进制行（git log %H 的标准形态），退而取首个足够长的十六进制串，
        /// 不再依赖固定行下标（旧实现 strarry[4] 在无 git 仓库时越界抛异常，构建直接崩溃）。
        /// </summary>
        static private string FindCommitHash(string cmdOutput, int lenth)
        {
            if (string.IsNullOrEmpty(cmdOutput))
            {
                return null;
            }

#if UNITY_EDITOR_OSX
            var t = cmdOutput.Trim();
            return (t.Length >= lenth && IsHex(t)) ? t.Substring(0, lenth) : null;
#elif UNITY_EDITOR_WIN
            var lines = cmdOutput.Split(new string[] { "\n" }, StringSplitOptions.None);
            string fallback = null;
            foreach (var line in lines)
            {
                var t = line.Trim();
                if (t.Length == 40 && IsHex(t))
                {
                    return t.Substring(0, lenth);
                }

                if (fallback == null && t.Length >= lenth && IsHex(t))
                {
                    fallback = t.Substring(0, lenth);
                }
            }
            return fallback;
#else
            return null;
#endif
        }

        /// <summary>
        /// 判断字符串是否全为十六进制字符（hash 合法性检查）
        /// </summary>
        static private bool IsHex(string s)
        {
            foreach (var c in s)
            {
                var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }
            return true;
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
