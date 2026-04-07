using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using BDFramework.Logs;
using Cysharp.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

[DefaultExecutionOrder(-10000)]
public class BDebug : MonoBehaviour
{
    /// <summary>
    ///启用宏
    /// </summary>
    public readonly static string ENABLE_BDEBUG = "ENABLE_BDEBUG";
    /// <summary>
    /// Ispector的log
    /// </summary>
    public bool IsLog = true;

    [Header("启用Log加密")]
    public bool EnablePlayerLogEncryption = true;

    [Tooltip("为空时使用默认密码；生产环境建议启动时覆盖")]
    public string PlayerLogEncryptPassword = LogCrypto.DEFAULT_PASSWORD;

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


    public class LogTag
    {
        public string Tag;
        public bool IsLog;
    }

    /// <summary>
    /// Enable的log tag
    /// </summary>
    public List<LogTag> DisableLogTagList = new List<LogTag>();

    private static bool IsConsoleLogEnabled
    {
        get { return inst == null || inst.IsLog; }
    }

    public static string PlayerLogRootPath
    {
        get
        {
#if UNITY_EDITOR
            return string.Empty;
#else
            return Persistence.LogRootDirectory;
#endif
        }
    }

    public static string CurrentPlayerLogFilePath
    {
        get
        {
#if UNITY_EDITOR
            return string.Empty;
#else
            return Persistence.CurrentFilePath;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitPlayerLogSerialize()
    {
#if !UNITY_EDITOR
        Persistence.Initialize(BuildPersistenceSettings());
#endif
    }

    private static PersistenceSettings BuildPersistenceSettings()
    {
        if (inst != null)
        {
            return new PersistenceSettings()
            {
                EnablePersistence = true,
                EnableEncryption = inst.EnablePlayerLogEncryption,
                EncryptPassword = inst.PlayerLogEncryptPassword,
            }.Normalize();
        }

        return PersistenceSettings.CreatePlayerDefault();
    }

    private static void ApplyPersistenceSettings()
    {
        if (Application.isPlaying && !Application.isEditor)
        {
            Persistence.Initialize(BuildPersistenceSettings());
        }
    }

    public static void FlushPlayerLogs()
    {
#if !UNITY_EDITOR
        Persistence.Flush();
#endif
    }

    public static string ExportPlayerLogToText(string binFilePath, string txtFilePath = null, string password = null)
    {
        return LogReader.ExportToText(binFilePath, txtFilePath, password);
    }

    /// <summary>
    /// 启动
    /// </summary>
    private void Awake()
    {
        if (inst != null && inst != this)
        {
            Destroy(gameObject);
            return;
        }

        inst = this;

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
            ApplyPersistenceSettings();
        }
    }

    private void OnApplicationQuit()
    {
#if !UNITY_EDITOR
        if (inst == this)
        {
            Persistence.Shutdown();
        }
#endif
    }

    private void OnDestroy()
    {
        if (inst == this)
        {
            inst = null;
        }
    }

    /// <summary>
    /// Log
    /// </summary>
    /// <param name="log"></param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(object log)
    {
        if (IsConsoleLogEnabled)
        {
            Debug.Log(log);
        }
    }

    /// <summary>
    /// Log
    /// 为了兼容以前 字符串color 
    /// </summary>
    /// <param name="tagOrLog">日志内容</param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(string tagOrLog, string logOrColor)
    {
        if (logOrColor.StartsWith("#")&&ColorUtility.TryParseHtmlString(logOrColor.Substring(1,logOrColor.Length-1), out var color))
        {
            Log(tagOrLog, color);
        }
        else
        {
            var log = ZString.Format("<color=#FFC63F>【{0}】</color> {1}", tagOrLog,  logOrColor);
            Debug.Log(log);
        }
    }

    /// <summary>
    /// Log
    /// </summary>
    /// <param name="log">日志内容</param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(string log, Color color)
    {
        if (IsConsoleLogEnabled)
        {
            log = ZString.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(color), log);
            Debug.Log(log);
        }
    }

    /// <summary>
    /// 根据tag进行Log
    /// 需要通过EnableTag()、DisableTag()管理
    /// </summary>
    /// <param name="tagOrLog">开关标记</param>
    /// <param name="log">日志内容</param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(string tagOrLog, string log, Color color)
    {
        var colorStr = ColorUtility.ToHtmlStringRGBA(color);
        Log(tagOrLog, log, colorStr);
    }

    /// <summary>
    /// 根据tag进行Log
    /// 需要通过EnableTag()、DisableTag()管理
    /// </summary>
    /// <param name="tagOrLog">开关标记</param>
    /// <param name="log">日志内容</param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(string tagOrLog, string log, string color)
    {
        if (IsEnableTag(tagOrLog))
        {
            log = ZString.Format("<color=#FFC63F>【{0}】</color> <color=#{1}>{2}</color>", tagOrLog, color, log);
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
        if (IsConsoleLogEnabled)
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
        if (IsConsoleLogEnabled)
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
        var owner = inst;
        if (owner == null)
        {
            return true;
        }

        lock (owner.DisableLogTagList)
        {
            var find = owner.DisableLogTagList.Find((t) => t.Tag == tag);
            if (find != null)
            {
                return find.IsLog;
            }
        }

        return true;
    }

    /// <summary>
    /// 打开某个log的tag
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    static public void DisableLog(string tag)
    {
        var owner = Inst;
        if (owner == null)
        {
            return;
        }

        lock (owner.DisableLogTagList)
        {
            var idx = owner.DisableLogTagList.FindIndex((t) => t.Tag == tag);
            if (idx < 0)
            {
                var log = new LogTag() { Tag = tag, IsLog = false };

                owner.DisableLogTagList.Add(log);

                idx = owner.DisableLogTagList.Count - 1;
            }

            owner.DisableLogTagList[idx].IsLog = false;
        }
    }

    /// <summary>
    /// 关闭tag的log
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    static public void EnableLog(string tag)
    {
        var owner = Inst;
        if (owner)
        {
            lock (owner.DisableLogTagList)
            {
                var idx = owner.DisableLogTagList.FindIndex((t) => t.Tag == tag);
                if (idx < 0)
                {
                    owner.DisableLogTagList.Add(new LogTag() { Tag = tag, IsLog = true });
                    idx = owner.DisableLogTagList.Count - 1;
                }

                owner.DisableLogTagList[idx].IsLog = true;
            }
        }
    }

    #endregion

    /// <summary>
    /// watch缓存
    /// </summary>
    static private readonly ConcurrentDictionary<string, Stopwatch> watchMap =
        new ConcurrentDictionary<string, Stopwatch>();


    /// <summary>
    /// 开始计时消耗，需要跟LogWatchEnd()成对调用
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_BDEBUG")]
    static public void LogWatchBegin(string watchTag)
    {
        var sw = new Stopwatch();
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
        if (watchMap.TryRemove(watchTag, out var sw))
        {
            sw.Stop();
            if (string.IsNullOrEmpty(color))
            {
                Debug.Log($"【{watchTag}】 耗时：<color=yellow>{sw.ElapsedTicks / 10000f} ms</color>");
            }
            else
            {
                Debug.Log(
                    $"<color={color}>【{watchTag}】</color> 耗时：<color=yellow>{sw.ElapsedTicks / 10000f} ms</color>");
            }
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
        if (watchMap.TryRemove(watchTag, out var sw))
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

        }
    }
}