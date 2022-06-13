using System;

namespace DotNetExtension
{
    /// <summary>
    /// datatime的扩展类
    /// </summary>
    static public class DateTimeEx
    {
        static System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区

        /// <summary>
        /// 获取从1970年开始的秒数
        /// </summary>
        /// <returns></returns>
        static public long GetTotalSeconds()
        {
            long timeStamp = (long) (DateTime.Now - startTime).TotalSeconds; // 相差秒数
            return timeStamp;
        }
    }
}
