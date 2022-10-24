using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;


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
            if (inst == null)
            {
                inst = new BDebug();
            }

            return inst;
        }
    }

    /// <summary>
    /// Ispector的log
    /// </summary>
    public bool IsLog = true;

    /// <summary>
    /// Enable的log tag
    /// </summary>
    public List<string> LogTagList = new List<string>();

    /// <summary>
    /// Enable的log tag
    /// </summary>
    public List<bool> LogTagEnableList = new List<bool>();

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
        if (Inst.IsLog)
        {
            Debug.Log(log);
        }

      
    }

    /// <summary>
    /// Log
    /// </summary>
    /// <param name="log"></param>
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
    /// </summary>
    /// <param name="log"></param>
    /// <param name="color">色号</param>
    [Conditional("ENABLE_BDEBUG")]
    public static void Log(string tag, object log, string color)
    {
        var idx = Inst.LogTagList.FindIndex((t) => t == tag);
        bool islog = false;
        if (idx >= 0)
        {
            islog = Inst.LogTagEnableList[idx];
        }

        if (islog)
        {
            log = ZString.Format("【{0}】<color={1}>{2}</color>",tag, (object) color, log);
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

    #region Tag相关的Log

    /// <summary>
    /// 打开某个log的tag
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    public void EnableLog(string tag)
    {
        var idx = Inst.LogTagList.FindIndex((t) => t == tag);
        if (idx >= 0)
        {
            Inst.LogTagEnableList[idx] = true;
        }
        else
        {
            Inst.LogTagList.Add(tag);
            Inst.LogTagEnableList.Add(true);
        }
    }

    /// <summary>
    /// 关闭tag的log
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    public void DisableTag()
    {
        var idx = Inst.LogTagList.FindIndex((t) => t == tag);
        if (idx >= 0)
        {
            Inst.LogTagEnableList[idx] = false;
        }
        else
        {
            Inst.LogTagList.Add(tag);
            Inst.LogTagEnableList.Add(false);
        }
    }

    #endregion

    [Conditional("ENABLE_BDEBUG")]
    static public void LogStopWatch(string tag)
    {
        
    }
   
}
