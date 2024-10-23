using System;
using System.Reflection;

namespace BDFramework.Editor.Unity3dEx
{
    public class DebugUtilImpl
    {
        delegate void LogPlayerBuildError(string msg, string fileName, int lineNum, int colomnNum);

        private static LogPlayerBuildError LogPlayerBuildError_impl;

        /// <summary>
        /// 打印脚本的行号
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="fileName"></param>
        /// <param name="lineNum"></param>
        /// <param name="colomnNum"></param>
        public static void Log(string msg, string fileName, int lineNum, int colomnNum)
        {
            if (LogPlayerBuildError_impl == null)
            {
                var unityLog = typeof(UnityEngine.Debug).GetMethod("LogPlayerBuildError", BindingFlags.NonPublic | BindingFlags.Static);
                LogPlayerBuildError_impl = Delegate.CreateDelegate(typeof(LogPlayerBuildError), unityLog) as LogPlayerBuildError;
            }
            LogPlayerBuildError_impl(msg, fileName, lineNum, colomnNum);
        }
    }
}