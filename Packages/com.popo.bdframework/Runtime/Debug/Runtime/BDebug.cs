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
        var idx = Inst.LogTagList.FindIndex((t) => t.Tag == tag);
        bool islog = false;
        if (idx >= 0)
        {
            islog = Inst.LogTagList[idx].IsLog;
        }

        if (islog)
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
        var idx = Inst.LogTagList.FindIndex((t) => t.Tag == tag);
        if (idx < 0)
        {
          Inst.LogTagList.Add(new LogTag(){ Tag =tag, IsLog = true});
          idx = Inst.LogTagList.Count;
        }
        
        Inst.LogTagList[idx].IsLog = true;
    }

    /// <summary>
    /// 关闭tag的log
    /// </summary>
    /// <returns></returns>
    [Conditional("ENABLE_BDEBUG")]
    public void DisableTag(string tag)
    {
        var idx = Inst.LogTagList.FindIndex((t) => t.Tag == tag);
        if (idx < 0)
        {
            Inst.LogTagList.Add(new LogTag(){ Tag =tag, IsLog = true});
            idx = Inst.LogTagList.Count;
        }
        
        Inst.LogTagList[idx].IsLog = false;
    }

    #endregion


    /// <summary>
    /// watch缓存
    /// </summary>
    static private Dictionary<string, Stopwatch> watchMap = new Dictionary<string, Stopwatch>();


    /// <summary>
    /// 开始打印Watch信息
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
    /// 打印Watch信息
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
                Debug.Log($"【{watchTag}】 耗时：{sw.ElapsedTicks / 10000f} ms");
            }
            else
            {
                Debug.Log($"<color={color}>【{watchTag}】 耗时：{sw.ElapsedTicks / 10000f} ms</color>");
            }

            watchMap.Remove(watchTag);
        }
    }
    
    /// <summary>
    /// 打印Watch信息
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_BDEBUG")]
    static public void LogWatchEnd(string logTag,string watchTag, string color = "")
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


#if UNITY_EDITOR

   /// <summary>
    /// Bdebug的编辑器
    /// </summary>
    [CustomEditor(typeof(BDebug))]
    public class BDebugEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var debug = target as BDebug;
         
            //log
            debug.IsLog = EditorGUILayout.Toggle("EnableLog", debug.IsLog);
            if (!Application.isPlaying)
            {
                if (debug.IsLog)
                {

                    EnableDebug();
                }
                else
                {
                   DisableDebug();
                }
            }
            //开启log与否
            debug.LogTagList.Sort((a, b) =>
            {
                //用tag排序
                return string.Compare(a.Tag, b.Tag);
            });
            //
            GUILayout.Label("Tag num:" +  debug.LogTagList.Count);
            foreach (var tag in debug.LogTagList)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Tag: "+tag.Tag,GUILayout.Width(80));

                    tag.IsLog = EditorGUILayout.Toggle(tag.IsLog);
                }
                GUILayout.EndHorizontal();
            }
        }

        
        /// <summary>
        /// 打开debug
        /// </summary>
        static public void EnableDebug()
        {
            //增加宏
            foreach (var bt in BApplication.SupportBuildTargetGroups)
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
                if (!symbols.Contains(BDebug.ENABLE_BDEBUG))
                {
                    string str = "";
                    if (!string.IsNullOrEmpty(symbols))
                    {
                        if (!str.EndsWith(";"))
                        {
                            str = symbols + ";" + BDebug.ENABLE_BDEBUG;
                        }
                        else
                        {
                            str = symbols + BDebug.ENABLE_BDEBUG;
                        }
                    }
                    else
                    {
                        str = BDebug.ENABLE_BDEBUG;
                    }


                    PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, str);
                }
            }
        }


        /// <summary>
        /// 关闭debug
        /// </summary>
        static public void DisableDebug()
        {
            //移除宏
            foreach (var bt in BApplication.SupportBuildTargetGroups)
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
                if (symbols.Contains(BDebug.ENABLE_BDEBUG + ";"))
                {
                    symbols = symbols.Replace(BDebug.ENABLE_BDEBUG + ";", "");
                }
                else if (symbols.Contains(BDebug.ENABLE_BDEBUG))
                {
                    symbols = symbols.Replace(BDebug.ENABLE_BDEBUG, "");
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, symbols);
            }
        }
    }

#endif
