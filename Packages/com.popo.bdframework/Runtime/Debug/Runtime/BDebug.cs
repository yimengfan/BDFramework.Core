using System.Collections.Generic;
using System.Diagnostics;
using BDFramework.Core.Tools;
using Cysharp.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BDebug : MonoBehaviour
{
    /// <summary>
    ///启用宏
    /// </summary>
    public readonly static string ENABLE_BDEBUG = "ENABLE_BDEBUG";

    //
    private static BDebug inst;

    private static BDebug Inst
    {
        get
        {
            if (inst == null && !Application.isPlaying)
            {
                inst = FindObjectOfType<BDebug>();
                if (!inst)
                {
                    inst = new GameObject("BDebug").AddComponent<BDebug>();
                }
            }

            return inst;
        }
    }

    /// <summary>
    /// Ispector的log
    /// </summary>
    public bool IsLog = true;

    public class LogTag
    {
        public string Tag;
        public bool IsLog;
    }

    /// <summary>
    /// Enable的log tag
    /// </summary>
    public List<LogTag> LogTagList = new List<LogTag>();

    /// <summary>
    /// 启动
    /// </summary>
    private void Awake()
    {
        inst = this;
    }

    /// <summary>
    /// Log
    /// </summary>
    /// <param name="log"></param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(object log)
    {
        if (Inst != null && Inst.IsLog)
        {
            Debug.Log(log);
        }
    }

    /// <summary>
    /// Log
    /// </summary>
    /// <param name="log">日志内容</param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(object log, string color)
    {
        if (Inst.IsLog)
        {
            log = ZString.Format("<color={0}>{1}</color>", (object) color, log);
            Debug.Log(log);
        }
    }

    /// <summary>
    /// 根据tag进行Log
    /// 需要通过EnableTag()、DisableTag()管理
    /// </summary>
    /// <param name="tag">开关标记</param>
    /// <param name="log">日志内容</param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(string tag, object log, string color = "white")
    {
        if (IsEnableTag(tag))
        {
            log = ZString.Format("【{0}】<color={1}>{2}</color>", tag, (object) color, log);
            Debug.Log(log);
        }
    }


    /// <summary>
    /// LogFormat
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    [Conditional("ENABLE_BDEBUG")]
    public static void LogFormat(string format, params object[] args)
    {
        if (Inst.IsLog)
        {
            Debug.LogFormat(format, args);
        }
    }

    /// <summary>
    /// LogFormat
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    [Conditional("ENABLE_BDEBUG")]
    public static void LogFormat(string tag, string format, params object[] args)
    {
        if (IsEnableTag(tag))
        {
            var log = string.Format(format, args);
            log = ZString.Format("【{0}】{1}", tag, log);
            Debug.Log(log);
        }
    }

    /// <summary>
    /// Log error
    /// </summary>
    /// <param name="log"></param>
    [Conditional("ENABLE_BDEBUG")]
    public static void LogError(object log)
    {
        if (Inst.IsLog)
        {
            Debug.LogError(log);
        }
    }

    /// <summary>
    /// Log error
    /// </summary>
    /// <param name="log"></param>
    [Conditional("ENABLE_BDEBUG")]
    public static void LogError(string tag, object log)
    {
        if (IsEnableTag(tag))
        {
            log = ZString.Format("【{0}】{1}", tag, log);
            Debug.LogError(log);
        }
    }


    #region Tag相关的Log

    /// <summary>
    /// 是否启用tag
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    static bool IsEnableTag(string tag)
    {
        var find = Inst.LogTagList.Find((t) => t.Tag == tag);
        if (find != null)
        {
            return find.IsLog;
        }

        return false;
    }

    /// <summary>
    /// 打开某个log的tag
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    static public void EnableLog(string tag)
    {
        var idx = Inst.LogTagList.FindIndex((t) => t.Tag == tag);
        if (idx < 0)
        {
            var log = new LogTag() {Tag = tag, IsLog = true};

            Inst.LogTagList.Add(log);

            idx = Inst.LogTagList.Count - 1;
        }

        Inst.LogTagList[idx].IsLog = true;
    }

    /// <summary>
    /// 关闭tag的log
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    static public void DisableTag(string tag)
    {
        var idx = Inst.LogTagList.FindIndex((t) => t.Tag == tag);
        if (idx < 0)
        {
            Inst.LogTagList.Add(new LogTag() {Tag = tag, IsLog = true});
            idx = Inst.LogTagList.Count - 1;
        }

        Inst.LogTagList[idx].IsLog = false;
    }

    #endregion

    /// <summary>
    /// watch缓存
    /// </summary>
    static private Dictionary<string, Stopwatch> watchMap = new Dictionary<string, Stopwatch>();


    /// <summary>
    /// 开始计时消耗，需要跟LogWatchEnd()成对调用
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_BDEBUG")]
    static public void LogWatchBegin(string watchTag)
    {
        Stopwatch sw = new Stopwatch();
        watchMap[watchTag] = sw;
        sw.Start();
    }

    /// <summary>
    /// 打印Watch计时信息
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_BDEBUG")]
    static public void LogWatchEnd(string watchTag, string color = "")
    {
        watchMap.TryGetValue(watchTag, out var sw);
        if (sw != null)
        {
            sw.Stop();
            if (string.IsNullOrEmpty(color))
            {
                Debug.Log($"【{watchTag}】 耗时：<color=yellow>{sw.ElapsedTicks / 10000f} ms</color>");
            }
            else
            {
                Debug.Log($"<color={color}>【{watchTag}】</color> 耗时：<color=yellow>{sw.ElapsedTicks / 10000f} ms</color>");
            }

            watchMap.Remove(watchTag);
        }
    }

    /// <summary>
    /// LogwatchEnd的tag版本
    ///  需要通过EnableTag()、DisableTag()管理
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_BDEBUG")]
    static public void LogWatchEnd(string logTag, string watchTag, string color = "")
    {
        watchMap.TryGetValue(watchTag, out var sw);

        if (sw != null)
        {
            sw.Stop();
            if (string.IsNullOrEmpty(color))
            {
                BDebug.Log(logTag, $"【{watchTag}】 耗时：{sw.ElapsedTicks / 10000f} ms");
            }
            else
            {
                BDebug.Log(logTag, $"<color={color}>【{watchTag}】 耗时：{sw.ElapsedTicks / 10000f} ms</color>");
            }

            watchMap.Remove(watchTag);
        }
    }
}



