using System;
using System.Diagnostics;
using UnityEngine;

namespace BDFramework.UnitTest
{
    static public class Assert
    {
        /// <summary>
        /// 判断是否为空
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        static public bool IsNull(object o, string failMessage = "Fail", float time = 0)
        {
            var ret = o == null;
            Log(!ret, failMessage, time);
            return ret;
        }

        /// <summary>
        /// 是否为false
        /// </summary>
        /// <returns></returns>
        static public bool IsPass(bool b, string failMessage = "Fail", float time = 0)
        {
            Log(b, failMessage, time);
            return b;
        }

        /// <summary>
        /// 是否为True
        /// </summary>
        /// <returns></returns>
        static public bool IsTrue(bool b, string failMessage = "Fail", float time = 0)
        {
            var ret = b;
            Log(ret, failMessage, time);
            return ret;
        }

        /// <summary>
        /// 是否为false
        /// </summary>
        /// <returns></returns>
        static public bool IsFalse(bool b, string failMessage = "Fail", float time = 0)
        {
            var ret = b == false;
            Log(ret, failMessage, time);
            return ret;
        }

        /// <summary>
        /// 比较值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="obj2"></param>
        static public bool Equals(object obj, object obj2, string failMessage = "Fail", float time = 0)
        {
            var ret = obj.Equals(obj2);
            Log(ret, failMessage, time);
            return ret;
        }


        private static bool IsFail = false;
        private static string FailMsg = "";
        private static float Time = 0;

        /// <summary>
        /// log
        /// </summary>
        /// <param name="ret"> true=成功，false=失败</param>
        /// <exception cref="Exception"></exception>
        static void Log(bool ret, string failMessage = "", float time = 0)
        {
            IsFail = !ret;
            FailMsg = failMessage;
            Time = time;
        }

        /// <summary>
        /// 获取成功失败状态 并且清除
        /// </summary>
        static public void GetAssertStaus(out bool ret, out string log, out float time)
        {
            ret = IsFail;
            log = FailMsg;
            time = Time;
        }

        /// <summary>
        /// 清理状态
        /// </summary>
        static public void ClearStatus()
        {
            IsFail = false;
            FailMsg = "";
            Time = 0;
        }

        #region 计时

        private static Stopwatch SW = new Stopwatch();

        /// <summary>
        /// 开始计时
        /// </summary>
        static public void StartWatch()
        {
            if (SW.IsRunning)
            {
                SW.Stop();
            }
            SW.Reset();
            SW.Start();
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        /// <returns>ms</returns>
        static public float StopWatch()
        {
            SW.Stop();
            return SW.ElapsedTicks / 10000f;
        }

        #endregion
    }
}
