using System;
using UnityEngine;

// Token: 0x02000002 RID: 2
public class BDebug : MonoBehaviour
{
    // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
    private void Awake()
    {
        BDebug.isLog = this.IsLog;
    }

    // Token: 0x06000002 RID: 2 RVA: 0x0000205D File Offset: 0x0000025D
    public static void Log(object s)
    {
        if (BDebug.isLog)
        {
            Debug.Log(s);
        }
    }

    // Token: 0x06000003 RID: 3 RVA: 0x0000206C File Offset: 0x0000026C
    public static void Log(object s, string color)
    {
        if (BDebug.isLog)
        {
            s = string.Format("<color={0}>{1}</color>", color, s);
            Debug.Log(s);
        }
    }

    // Token: 0x06000004 RID: 4 RVA: 0x00002089 File Offset: 0x00000289
    public static void LogFormat(string format, params object[] args)
    {
        if (BDebug.isLog)
        {
            Debug.LogFormat(format, args);
        }
    }

    // Token: 0x06000005 RID: 5 RVA: 0x00002099 File Offset: 0x00000299
    public static void LogError(object s)
    {
        if (BDebug.isLog)
        {
            Debug.LogError(s);
        }
    }

    // Token: 0x06000006 RID: 6 RVA: 0x00002050 File Offset: 0x00000250
    private void Update()
    {
        BDebug.isLog = this.IsLog;
    }

    // Token: 0x04000001 RID: 1
    public bool IsLog;

    // Token: 0x04000002 RID: 2
    private static bool isLog;
}