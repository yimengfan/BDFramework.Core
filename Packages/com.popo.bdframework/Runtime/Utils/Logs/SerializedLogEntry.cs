using System;
using UnityEngine;

namespace BDFramework.Logs
{
    public struct SerializedLogEntry
    {
        public long UtcTicks;
        public int ThreadId;
        public LogType LogType;
        public string Message;
        public string StackTrace;

        public DateTime LocalTime
        {
            get { return new DateTime(this.UtcTicks, DateTimeKind.Utc).ToLocalTime(); }
        }
    }
}

