using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Installer
{
    public static class BashUtil
    {
        public static int RunCommand(string workingDir, string program, string[] args, bool log = true)
        {
            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = workingDir;
                p.StartInfo.FileName = program;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                string argsStr = string.Join(" ", args.Select(arg => "\"" + arg + "\""));
                p.StartInfo.Arguments = argsStr;
                if (log)
                {
                    UnityEngine.Debug.Log($"[BashUtil] run => {program} {argsStr}");
                }
                p.Start();
                p.WaitForExit();
                return p.ExitCode;
            }
        }


        public static (int ExitCode, string StdOut, string StdErr) RunCommand2(string workingDir, string program, string[] args, bool log = true)
        {
            using (Process p = new Process())
            {
                p.StartInfo.WorkingDirectory = workingDir;
                p.StartInfo.FileName = program;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                string argsStr = string.Join(" ", args);
                p.StartInfo.Arguments = argsStr;
                if (log)
                {
                    UnityEngine.Debug.Log($"[BashUtil] run => {program} {argsStr}");
                }
                p.Start();
                p.WaitForExit();

                string stdOut = p.StandardOutput.ReadToEnd();
                string stdErr = p.StandardError.ReadToEnd();
                return (p.ExitCode, stdOut, stdErr);
            }
        }


        public static void RemoveDir(string dir, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"[BashUtil] RemoveDir dir:{dir}");
            }

            int maxTryCount = 5;
            for (int i = 0; i < maxTryCount; ++i)
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        return;
                    }
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    foreach (var subDir in Directory.GetDirectories(dir))
                    {
                        RemoveDir(subDir);
                    }
                    Directory.Delete(dir, true);
                    break;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[BashUtil] RemoveDir:{dir} with exception:{e}. try count:{i}");
                    Thread.Sleep(100);
                }
            }
        }

        public static void RecreateDir(string dir)
        {
            if(Directory.Exists(dir))
            {
                RemoveDir(dir, true);
            }
            Directory.CreateDirectory(dir);
        }
        public static void CopyDir(string src, string dst, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"[BashUtil] CopyDir {src} => {dst}");
            }
            if (Directory.Exists(dst))
            {
                RemoveDir(dst);
            }
            else
            {
                string parentDir = Path.GetDirectoryName(Path.GetFullPath(dst));
                Directory.CreateDirectory(parentDir);
            }

            UnityEditor.FileUtil.CopyFileOrDirectory(src, dst);
        }
    }
}
